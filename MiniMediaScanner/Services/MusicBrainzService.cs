using System.Diagnostics;
using MiniMediaScanner.Callbacks;
using MiniMediaScanner.Callbacks.Status;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models.MusicBrainz;
using MiniMediaScanner.Repositories;

namespace MiniMediaScanner.Services;

public class MusicBrainzService
{
    public readonly int PreventUpdateWithinDays; 
    
    private readonly MusicBrainzAPIService _musicBrainzApiService;
    private const int BulkRequestLimit = 100;
    private readonly MusicBrainzArtistRepository _musicBrainzArtistRepository;
    private readonly MusicBrainzReleaseRepository _musicBrainzReleaseRepository;
    private readonly MusicBrainzReleaseTrackRepository _musicBrainzReleaseTrackRepository;
    private readonly MusicBrainzAreaRepository _musicBrainzAreaRepository;
    private readonly MusicBrainzLabelRepository _musicBrainzLabelRepository;
    private readonly MusicBrainzReleaseArtistRepository _musicBrainzReleaseArtistRepository;
    private readonly MusicBrainzReleaseLabelRepository _musicBrainzReleaseLabelRepository;
    
    public MusicBrainzService(string connectionString, int preventUpdateWithinDays)
    {
        this.PreventUpdateWithinDays =  preventUpdateWithinDays;
        _musicBrainzApiService = new MusicBrainzAPIService();
        _musicBrainzArtistRepository = new MusicBrainzArtistRepository(connectionString);
        _musicBrainzReleaseRepository = new MusicBrainzReleaseRepository(connectionString);
        _musicBrainzReleaseTrackRepository = new MusicBrainzReleaseTrackRepository(connectionString);
        _musicBrainzAreaRepository = new MusicBrainzAreaRepository(connectionString);
        _musicBrainzLabelRepository = new MusicBrainzLabelRepository(connectionString);
        _musicBrainzReleaseArtistRepository = new MusicBrainzReleaseArtistRepository(connectionString);
        _musicBrainzReleaseLabelRepository = new MusicBrainzReleaseLabelRepository(connectionString);
    }

    public async Task InsertMissingMusicBrainzArtistAsync(MetadataInfo metadataInfo,
        Action<UpdateMBCallback> callback = null)
    {
        if (!string.IsNullOrWhiteSpace(metadataInfo.MusicBrainzArtistId))
        {
            foreach (var artistId in metadataInfo.MusicBrainzArtistId
                         .Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                await UpdateMusicBrainzArtistAsync(artistId, callback);
            }
        }
    }
    
    public async Task UpdateMusicBrainzArtistAsync(string musicBrainzArtistId, 
        Action<UpdateMBCallback> callback = null)
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
                await ProcessMusicBrainzArtistAsync(artistId, callback);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"{ex.Message}");
        }
    }

    private async Task ProcessMusicBrainzArtistAsync(string musicBrainzArtistId, 
        Action<UpdateMBCallback> callback = null)
    {
        if (!Guid.TryParse(musicBrainzArtistId, out var musicBrainzArtistGuid))
        {
            return;
        }
        
        DateTime lastSyncTime = await _musicBrainzArtistRepository.GetArtistLastSyncTimeAsync(musicBrainzArtistGuid);
        if (DateTime.Now.Subtract(lastSyncTime).TotalDays < this.PreventUpdateWithinDays)
        {
            callback?.Invoke(new UpdateMBCallback(musicBrainzArtistGuid, UpdateMBStatus.SkippedSyncedWithin));
            Debug.WriteLine($"Skipped synchronizing for MusicBrainzArtistId '{musicBrainzArtistId}' synced already within {this.PreventUpdateWithinDays}days");
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
        
        await _musicBrainzArtistRepository.UpsertArtistAsync(
            musicBrainzArtistGuid, 
            musicBrainzArtistInfo.Name, 
            musicBrainzArtistInfo.Type,
            musicBrainzArtistInfo.Country,
            musicBrainzArtistInfo.SortName,
            musicBrainzArtistInfo.Disambiguation);


        UpdateMBCallback updateMbCallback = new UpdateMBCallback(musicBrainzArtistGuid, UpdateMBStatus.Updating);
        updateMbCallback.Albums = new List<MusicBrainzArtistReleaseModel>();
        updateMbCallback.Artist = musicBrainzArtistInfo;
        
        int offset = 0;
        while (true)
        {
            var releases = await _musicBrainzApiService.GetReleasesForArtistAsync(musicBrainzArtistGuid, BulkRequestLimit, offset);

            if (releases?.Releases?.Count == 0)
            {
                break;
            }

            offset += BulkRequestLimit;
            
            updateMbCallback.Albums.AddRange(releases.Releases);
        
            foreach (var release in releases.Releases)
            {
                updateMbCallback.CurrentAlbum = release;
                updateMbCallback.Progress++;
                callback?.Invoke(updateMbCallback);
                
                if (!Guid.TryParse(release.Id, out var releaseId) ||
                    string.IsNullOrWhiteSpace(release.Title))
                {
                    continue;
                }

                if (await _musicBrainzReleaseRepository.ReleaseIdExistsAsync(releaseId))
                {
                    continue;
                }
                
                var releaseRecordings = await _musicBrainzApiService.GetReleasesWithRecordingsForArtistAsync(releaseId, BulkRequestLimit, 0);
                
                await _musicBrainzReleaseRepository.UpsertReleaseAsync(musicBrainzArtistGuid, releaseId, release.Title, release.Status, 
                    release.StatusId, release.Date, release.Barcode, release.Country, release.Disambiguation, release.Quality);

                if (releaseRecordings?.LabeLInfo != null)
                {
                    foreach (var label in releaseRecordings.LabeLInfo)
                    {
                        if (!Guid.TryParse(label?.Label?.Id, out var labelId))
                        {
                            continue;
                        }

                        await _musicBrainzReleaseLabelRepository.UpsertReleaseLabelAsync(releaseId, labelId, label?.CataLogNumber ?? string.Empty);

                        if (await _musicBrainzLabelRepository.LabelExistsAsync(labelId))
                        {
                            continue;
                        }
                        var allLabelInfo = await _musicBrainzApiService.GetLabelByIdAsync(labelId);

                        if (allLabelInfo == null)
                        {
                            continue;
                        }
                        
                        Guid.TryParse(allLabelInfo?.Area?.Id, out var areaId);

                        await _musicBrainzLabelRepository.UpsertLabelAsync(labelId,
                            areaId,
                            allLabelInfo.Name,
                            allLabelInfo.Disambiguation ?? string.Empty,
                            allLabelInfo.LabelCode ?? 0,
                            allLabelInfo.Type ?? string.Empty,
                            allLabelInfo.LifeSpan?.Begin ?? string.Empty,
                            allLabelInfo.LifeSpan?.End ?? string.Empty,
                            allLabelInfo.LifeSpan?.Ended ?? false,
                            allLabelInfo.SortName ?? string.Empty,
                            allLabelInfo.TypeId ?? string.Empty,
                            allLabelInfo.Country ?? string.Empty
                        );

                        if (GuidHelper.GuidHasValue(areaId))
                        {
                            await _musicBrainzAreaRepository.UpsertAreaAsync(areaId,
                                allLabelInfo.Area.Name,
                                allLabelInfo.Area.Type,
                                allLabelInfo.Area.TypeId,
                                allLabelInfo.Area.SortName,
                                allLabelInfo.Area.Disambiguation);
                        }
                    }
                }
                
                foreach (var media in releaseRecordings?.Media ?? [])
                {
                    foreach (var track in media?.Tracks ?? [])
                    {
                        if (!Guid.TryParse(track.Id, out var trackId) ||
                            !Guid.TryParse(track?.Recording?.Id, out var trackRecordingId))
                        {
                            continue;
                        }

                        int index = 0;
                        foreach(var credit in track.Recording.ArtistCredit)
                        {
                            if (Guid.TryParse(credit.Artist?.Id, out var artistId))
                            {
                                if (!await _musicBrainzArtistRepository.ArtistExistsByIdAsync(artistId))
                                {
                                    await _musicBrainzArtistRepository.UpsertArtistAsync(artistId,
                                        credit.Artist.Name ?? string.Empty,
                                        credit.Artist.Type,
                                        credit.Artist.Country,
                                        credit.Artist.SortName,
                                        credit.Artist.Disambiguation);
                                }
                                
                                await _musicBrainzReleaseArtistRepository.UpsertReleaseTrackArtistAsync(
                                    trackId, artistId, credit.JoinPhrase, index);
                            }
                            index++;
                        }
                        
                        await _musicBrainzReleaseTrackRepository.UpsertReleaseTrackAsync(trackId, 
                                                                                         trackRecordingId, 
                                                                                         track.Title ?? string.Empty, 
                                                                                         release?.Status ?? string.Empty, 
                                                                                         release?.StatusId ?? string.Empty, 
                                                                                         releaseId,
                                                                                         track.Length ?? 0,
                                                                                         track.Number ?? 0,
                                                                                         track.Position ?? 0,
                                                                                         trackRecordingId,
                                                                                         track.Recording.Length ?? 0,
                                                                                         track.Recording.Title ?? string.Empty,
                                                                                         track.Recording.Video,
                                                                                         media?.TrackCount ?? 0,
                                                                                         media?.Format ?? string.Empty,
                                                                                         media?.Title ?? string.Empty,
                                                                                         media?.Position ?? 0,
                                                                                         media?.TrackOffset ?? 0);
                    }
                }
            }

            if (releases.Releases.Count != BulkRequestLimit)
            {
                //limit reached already, no need to loop again to ask MusicBrainz
                break;
            }
        }

        await _musicBrainzArtistRepository.SetArtistLastSyncTimeAsync(musicBrainzArtistGuid);
    }
}