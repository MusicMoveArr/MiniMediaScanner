using MiniMediaScanner.Repositories;

namespace MiniMediaScanner.Services;

public class MusicBrainzService
{
    private readonly MusicBrainzAPIService _musicBrainzApiService;
    private const int BulkRequestLimit = 100;
    private readonly MusicBrainzArtistRepository _musicBrainzArtistRepository;
    private readonly MusicBrainzReleaseRepository _musicBrainzReleaseRepository;
    private readonly MusicBrainzReleaseTrackRepository _musicBrainzReleaseTrackRepository;
    
    public MusicBrainzService(string connectionString)
    {
        _musicBrainzApiService = new MusicBrainzAPIService();
        _musicBrainzArtistRepository = new MusicBrainzArtistRepository(connectionString);
        _musicBrainzReleaseRepository = new MusicBrainzReleaseRepository(connectionString);
        _musicBrainzReleaseTrackRepository = new MusicBrainzReleaseTrackRepository(connectionString);
    }

    public async Task InsertMissingMusicBrainzArtistAsync(MetadataInfo metadataInfo)
    {
        if (!string.IsNullOrWhiteSpace(metadataInfo.MusicBrainzArtistId))
        {
            foreach (var artistId in metadataInfo.MusicBrainzArtistId
                         .Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                await UpdateMusicBrainzArtistAsync(artistId, true);
            }
        }
    }
    
    public async Task UpdateMusicBrainzArtistAsync(string musicBrainzArtistId, bool updateExisting = false)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(musicBrainzArtistId))
            {
                return;
            }

            string[] musicBrainzArtistIds = musicBrainzArtistId.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (string artistId in musicBrainzArtistIds)
            {
                await ProcessMusicBrainzArtistAsync(artistId, updateExisting);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.Message}");
        }
    }

    private async Task ProcessMusicBrainzArtistAsync(string musicBrainzArtistId, bool updateExisting = false)
    {
        try
        {
            if (!updateExisting && (await _musicBrainzArtistRepository.GetRemoteMusicBrainzArtistIdAsync(musicBrainzArtistId)).HasValue)
            {
                return;
            }

            if (!Guid.TryParse(musicBrainzArtistId, out var musicBrainzArtistGuid))
            {
                return;
            }
            
            DateTime lastSyncTime = await _musicBrainzArtistRepository.GetBrainzArtistLastSyncTimeAsync(musicBrainzArtistGuid);

            if (DateTime.Now.Subtract(lastSyncTime).TotalDays < 7)
            {
                Console.WriteLine($"Skipped synchronizing for MusicBrainzArtistId '{musicBrainzArtistId}' synced already within 7days");
                return;
            }
            
            var musicBrainzArtistInfo = await _musicBrainzApiService.GetArtistInfoAsync(musicBrainzArtistGuid);

            if (musicBrainzArtistInfo == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(musicBrainzArtistInfo.Name))
            {
                return;
            }
            
            Guid? artistDbId = await _musicBrainzArtistRepository.InsertMusicBrainzArtistAsync(musicBrainzArtistGuid, 
                musicBrainzArtistInfo.Name, 
                musicBrainzArtistInfo.Type,
                musicBrainzArtistInfo.Country,
                musicBrainzArtistInfo.SortName,
                musicBrainzArtistInfo.Disambiguation);

            int offset = 0;
            while (true)
            {
                var releases = await _musicBrainzApiService.GetReleasesForArtistAsync(musicBrainzArtistGuid, BulkRequestLimit, offset);

                if (releases?.Releases?.Count == 0)
                {
                    break;
                }

                offset += BulkRequestLimit;
            
                foreach (var release in releases.Releases)
                {
                    if (!Guid.TryParse(release.Id, out var releaseId) ||
                        string.IsNullOrWhiteSpace(release.Title))
                    {
                        continue;
                    }
                    
                    var releaseRecordings = await _musicBrainzApiService.GetReleasesWithRecordingsForArtistAsync(releaseId, BulkRequestLimit, 0);
                    
                    await _musicBrainzReleaseRepository.InsertMusicBrainzReleaseAsync(artistDbId.Value, releaseId, release.Title, release.Status, 
                        release.StatusId, release.Date, release.Barcode, release.Country, release.Disambiguation, release.Quality);

                    foreach (var media in releaseRecordings.Media)
                    {
                        foreach (var track in media.Tracks)
                        {
                            if (!Guid.TryParse(track.Id, out var trackId) ||
                                !Guid.TryParse(track?.Recording?.Id, out var trackRecordingId))
                            {
                                continue;
                            }
                            
                            await _musicBrainzReleaseTrackRepository.InsertMusicBrainzReleaseTrackAsync(trackId, 
                                                                                             trackRecordingId, 
                                                                                             track.Title ?? string.Empty, 
                                                                                             release.Status, 
                                                                                             releaseId,
                                                                                             track.Length ?? 0,
                                                                                             track.Number ?? 0,
                                                                                             track.Position ?? 0,
                                                                                             trackRecordingId,
                                                                                             track.Recording.Length ?? 0,
                                                                                             track.Recording.Title ?? string.Empty,
                                                                                             track.Recording.Video,
                                                                                             media.TrackCount ?? 0,
                                                                                             media.Format ?? string.Empty,
                                                                                             media.Title ?? string.Empty,
                                                                                             media.Position ?? 0,
                                                                                             media.TrackOffset ?? 0);
                        }
                    }
                }

                if (releases.Releases.Count != BulkRequestLimit)
                {
                    //limit reached already, no need to loop again to ask MusicBrainz
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}