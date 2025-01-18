using MiniMediaScanner.Repositories;

namespace MiniMediaScanner.Services;

public class MusicBrainzService
{
    private readonly MusicBrainzAPIService _musicBrainzApiService;
    private readonly List<string> _musicBrainzIds = new List<string>();
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

    public void InsertMissingMusicBrainzArtist(MetadataInfo metadataInfo)
    {
        metadataInfo.MusicBrainzArtistId
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .ToList()
            .ForEach(artistId => UpdateMusicBrainzArtist(artistId, true));
    }
    
    public void UpdateMusicBrainzArtist(string musicBrainzArtistId, bool updateExisting = false)
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
                ProcessMusicBrainzArtist(artistId, updateExisting);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.Message}");
        }
    }

    private void ProcessMusicBrainzArtist(string musicBrainzArtistId, bool updateExisting = false)
    {
        try
        {
            if (_musicBrainzIds.Contains(musicBrainzArtistId) ||
                (!updateExisting && _musicBrainzArtistRepository.GetRemoteMusicBrainzArtist(musicBrainzArtistId).HasValue))
            {
                return;
            }
            _musicBrainzIds.Add(musicBrainzArtistId);
            
            var musicBrainzArtistInfo = _musicBrainzApiService.GetArtistInfo(musicBrainzArtistId);

            if (musicBrainzArtistInfo == null)
            {
                return;
            }
            
            Guid? artistDbId = _musicBrainzArtistRepository.InsertMusicBrainzArtist(musicBrainzArtistId, 
                musicBrainzArtistInfo.Name, 
                musicBrainzArtistInfo.Type,
                musicBrainzArtistInfo.Country,
                musicBrainzArtistInfo.SortName,
                musicBrainzArtistInfo.Disambiguation);

            int offset = 0;
            while (true)
            {
                var releases = _musicBrainzApiService.GetReleasesForArtist(musicBrainzArtistId, BulkRequestLimit, offset);

                if (releases?.Releases?.Count == 0)
                {
                    break;
                }

                offset += BulkRequestLimit;
            
                foreach (var release in releases.Releases)
                {
                    _musicBrainzReleaseRepository.InsertMusicBrainzRelease(artistDbId.Value.ToString(), release.Id, release.Title, release.Status, 
                        release.StatusId, release.Date, release.Barcode, release.Country, release.Disambiguation, release.Quality);

                    foreach (var media in release.Media)
                    {
                        foreach (var track in media.Tracks)
                        {
                            _musicBrainzReleaseTrackRepository.InsertMusicBrainzReleaseTrack(track.Id, track.Recording.Id, track.Title, release.Status, release.Id);
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