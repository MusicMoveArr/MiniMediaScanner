using ATL;
using FuzzySharp;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models.AcoustId;
using MiniMediaScanner.Models.MusicBrainz;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class TagMissingMetadataCommandHandler
{
    private readonly AcoustIdService _acoustIdService;
    private readonly MusicBrainzAPIService _musicBrainzApiService;
    private readonly MetadataRepository _metadataRepository;
    private readonly ArtistRepository _artistRepository;
    private readonly MediaTagWriteService _mediaTagWriteService;
    private readonly ImportCommandHandler _importCommandHandler;
    private readonly MusicBrainzArtistRepository _musicBrainzArtistRepository;
    private readonly FileMetaDataService _fileMetaDataService;

    public string AcoustId { get; set; }
    public bool Write { get; set; }
    public string ArtistFilter { get; set; }
    public string AlbumFilter { get; set; }
    public bool OverwriteTag { get; set; }
    public int MatchPercentageTags { get; set; }
    public int MatchPercentageAcoustId { get; set; }

    public TagMissingMetadataCommandHandler(string connectionString)
    {
        _acoustIdService = new AcoustIdService();
        _musicBrainzApiService = new MusicBrainzAPIService();
        _metadataRepository = new MetadataRepository(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
        _mediaTagWriteService = new MediaTagWriteService();
        _importCommandHandler = new ImportCommandHandler(connectionString);
        _musicBrainzArtistRepository = new MusicBrainzArtistRepository(connectionString);
        _fileMetaDataService = new FileMetaDataService();
    }
    
    public async Task TagMetadataAsync()
    {
        await ParallelHelper.ForEachAsync(await _artistRepository.GetAllArtistNamesLowercaseUniqueAsync(), 4, async artist =>
        {
            try
            {
                await TagMetadataAsync(artist);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        });
    }

    public async Task TagMetadataAsync(string artist)
    {
        var metadata = (await _metadataRepository.GetMissingMusicBrainzMetadataRecordsAsync(artist))
            .Where(metadata => string.IsNullOrWhiteSpace(AlbumFilter) || 
                               string.Equals(metadata.Album, AlbumFilter, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Console.WriteLine($"Checking artist '{artist}', found {metadata.Count} tracks to process");

        foreach (var record in metadata.Where(r => new FileInfo(r.Path).Exists))
        {
            try
            {
                await ProcessFileAsync(record);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
                
    private async Task ProcessFileAsync(MetadataInfo metadata)
    {
        AcoustIdResponse? acoustIdLookup = await _acoustIdService.LookupAcoustIdAsync(AcoustId,
            metadata.Tag_AcoustIdFingerPrint, (int)metadata.Tag_AcoustIdFingerPrint_Duration);
        
        var matchedRecording = await GetBestMatchingAcoustIdAsync(acoustIdLookup, 
            metadata.Artist, 
            metadata.Album, 
            metadata.Title,
            (int)TimeSpan.Parse(metadata.Tag_Length).TotalSeconds, 
            MatchPercentageAcoustId);

        if (matchedRecording == null)
        {
            Console.WriteLine($"No recording Id found from AcoustId for '{metadata.Path}'");
            return;
        }
        
        Guid recordingId = matchedRecording?.Id ?? Guid.Empty;
        Guid acoustId = matchedRecording?.AcoustId ?? Guid.Empty;

        Track track = null;
        MusicBrainzArtistModel? artistModel = null;
        
        if (!GuidHelper.GuidHasValue(recordingId))
        {
            Console.WriteLine($"No recording ID found from AcoustId for '{metadata.Path}'");
            
            track = new Track(metadata.Path);
            recordingId = (await _musicBrainzArtistRepository.GetRecordingIdByNameAsync(track.Artist, track.Album, track.Title)).Value;

            if (GuidHelper.GuidHasValue(recordingId))
            {
                artistModel = await _musicBrainzApiService.GetRecordingByIdAsync(recordingId);
                if (artistModel != null)
                {
                    Console.WriteLine($"Found MusicBrainz data from the database, '{metadata.Path}'");
                }
            }
        }
        else
        {
            artistModel = await _musicBrainzApiService.GetRecordingByIdAsync(recordingId);
        }

        if (track == null)
        {
            track = new Track(metadata.Path);
        }
        
        //grab the best matched release
        string? artistCountry = !string.IsNullOrWhiteSpace(track.Artist) ? await _musicBrainzArtistRepository.GetArtistCountryByNameAsync(track.Artist) : string.Empty;
        string trackBarcode = _mediaTagWriteService.GetTagValue(track, "barcode");
        var matchedReleases =
            artistModel?.Releases
                .Where(release => release.Media?.Any() == true && release.Media?.First()?.Tracks?.Any() == true)
                .Select(release => new
                {
                    Album = release.Title,
                    release.Media?.First()?.Tracks?.First().Title,
                    Length = release.Media?.First()?.Tracks?.First()?.Length / 1000 ?? int.MaxValue,
                    release.Barcode,
                    Release = release
                })
                .Where(release => !string.IsNullOrWhiteSpace(release.Album) && !string.IsNullOrWhiteSpace(release.Title) && !string.IsNullOrWhiteSpace(release.Release.Country))
                .Select(release => new
                {
                    Release = release.Release,
                    release.Title,
                    AlbumMatch = Fuzz.Ratio(release.Album, track.Album),
                    TitleMatch = Fuzz.Ratio(release.Title, track.Title),
                    LengthMatch = Math.Abs(track.Duration - release.Length),
                    CountryMatch = !string.IsNullOrWhiteSpace(artistCountry) ? Fuzz.Ratio(release.Release.Country, artistCountry) : 0,
                    BarcodeMatch = !string.IsNullOrWhiteSpace(release.Barcode) ? Fuzz.Ratio(release.Barcode, trackBarcode) : 0
                })
                .Where(match => match.AlbumMatch > MatchPercentageTags)
                .Where(match => match.TitleMatch > MatchPercentageTags)
                .Where(match => FuzzyHelper.ExactNumberMatch(match.Release.Title, track.Album))
                .Where(match => FuzzyHelper.ExactNumberMatch(match.Title, track.Title))
                .Where(match => match.TitleMatch > MatchPercentageTags)
                .OrderByDescending(match => match.AlbumMatch)
                .ThenByDescending(match => match.TitleMatch)
                .ThenByDescending(match => match.CountryMatch)
                .ThenBy(match => match.LengthMatch)
                .ThenByDescending(match => match.BarcodeMatch)
                .ToList();

        var firstMatch = matchedReleases?.FirstOrDefault();
        MusicBrainzArtistReleaseModel? release = matchedReleases?.FirstOrDefault()?.Release;
        
        if (artistModel == null || release == null)
        {
            return;
        }
        
        Console.WriteLine($"Release found for '{metadata.Path}'" +
                          $", Title '{release.Title}'" +
                          $", Date '{release.Date}'" +
                          $", Barcode '{release.Barcode}'" +
                          $", Country '{release.Country}'" +
                          $", Album match: {firstMatch.AlbumMatch}%" +
                          $", Title match: {firstMatch.TitleMatch}%" +
                          $", Barcode match: {firstMatch.BarcodeMatch}%"+
                          $", Country match: {firstMatch.CountryMatch}%");

        if (!this.Write)
        {
            return;
        }

        if (!GuidHelper.GuidHasValue(recordingId))
        {
            Guid.TryParse(release.Media?.FirstOrDefault()?.Tracks?.FirstOrDefault()?.Recording?.Id, out recordingId);
        }
        
        var metadataInfo = await _fileMetaDataService.GetMetadataInfoAsync(new FileInfo(track.Path));
        bool trackInfoUpdated = false;
        string? musicBrainzTrackId = release.Media?.FirstOrDefault()?.Tracks?.FirstOrDefault()?.Id;

        //grab the best matching Artist based on the name from the AlbumArtist media tag
        var bestMatchedArtist =
            !string.IsNullOrWhiteSpace(track.AlbumArtist)
                ? artistModel?.ArtistCredit
                    .Select(artist => new
                    {
                        Artist = artist,
                        MatchedFor = Fuzz.Ratio(artist.Name, track.AlbumArtist)
                    })
                    .OrderByDescending(match => match.MatchedFor)
                    .Select(match => match.Artist)
                    .FirstOrDefault()
                : artistModel?.ArtistCredit?.FirstOrDefault();
        
        string? musicBrainzReleaseArtistId = bestMatchedArtist?.Artist?.Id;
        string? musicBrainzAlbumId = release.Id;
        string? musicBrainzReleaseGroupId = release.ReleaseGroup.Id;
        
        string artists = string.Join(';', artistModel?.ArtistCredit.Select(artist => artist.Name));
        string musicBrainzArtistIds = string.Join(';', artistModel?.ArtistCredit.Select(artist => artist.Artist.Id));
        string isrcs = artistModel?.ISRCS != null ? string.Join(';', artistModel?.ISRCS) : string.Empty;

        if (Guid.TryParse(release.Id, out var releaseId))
        {
            MusicBrainzArtistReleaseModel? withLabeLInfo = await _musicBrainzApiService.GetReleaseWithLabelAsync(releaseId);
            var label = withLabeLInfo?.LabeLInfo?.FirstOrDefault(label => label?.Label?.Type?.ToLower().Contains("production") == true);

            if (label == null && withLabeLInfo?.LabeLInfo?.Count == 1)
            {
                label = withLabeLInfo?.LabeLInfo?.FirstOrDefault();
            }
            if (!string.IsNullOrWhiteSpace(label?.Label?.Name))
            {
                _mediaTagWriteService.UpdateTag(track, metadataInfo, "LABEL", label?.Label.Name, ref trackInfoUpdated, OverwriteTag);
                _mediaTagWriteService.UpdateTag(track, metadataInfo, "CATALOGNUMBER", label?.CataLogNumber, ref trackInfoUpdated, OverwriteTag);
            }
        }
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "date", release.Date, ref trackInfoUpdated, OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "originaldate", release.Date, ref trackInfoUpdated, OverwriteTag);

        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Title", release.Media?.FirstOrDefault()?.Title, ref trackInfoUpdated, OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Album", release.Title, ref trackInfoUpdated, OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "AlbumArtist", bestMatchedArtist?.Name, ref trackInfoUpdated, OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Artist", bestMatchedArtist?.Name, ref trackInfoUpdated, OverwriteTag);

        _mediaTagWriteService.UpdateTag(track, metadataInfo, "ARTISTS", artists, ref trackInfoUpdated, OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "ISRC", isrcs, ref trackInfoUpdated, OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "SCRIPT", release?.TextRepresentation?.Script, ref trackInfoUpdated, OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "barcode", release.Barcode, ref trackInfoUpdated, OverwriteTag);

        _mediaTagWriteService.UpdateTag(track, metadataInfo, "MusicBrainz Artist Id", musicBrainzArtistIds, ref trackInfoUpdated, OverwriteTag);

        if (GuidHelper.GuidHasValue(recordingId))
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "MusicBrainz Track Id", recordingId.ToString(), ref trackInfoUpdated, OverwriteTag);
        }
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "MusicBrainz Release Track Id", musicBrainzTrackId, ref trackInfoUpdated, OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "MusicBrainz Release Artist Id", musicBrainzReleaseArtistId, ref trackInfoUpdated, OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "MusicBrainz Release Group Id", musicBrainzReleaseGroupId, ref trackInfoUpdated, OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "MusicBrainz Release Id", release.Id, ref trackInfoUpdated, OverwriteTag);

        _mediaTagWriteService.UpdateTag(track, metadataInfo, "MusicBrainz Album Artist Id", musicBrainzArtistIds, ref trackInfoUpdated, OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "MusicBrainz Album Id", musicBrainzAlbumId, ref trackInfoUpdated, OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "MusicBrainz Album Type", release.ReleaseGroup.PrimaryType, ref trackInfoUpdated, OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "MusicBrainz Album Release Country", release.Country, ref trackInfoUpdated, OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "MusicBrainz Album Status", release.Status, ref trackInfoUpdated, OverwriteTag);

        if (GuidHelper.GuidHasValue(acoustId))
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "Acoustid Id", acoustId.ToString(), ref trackInfoUpdated, OverwriteTag);
        }
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Date", release.ReleaseGroup.FirstReleaseDate, ref trackInfoUpdated, OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "originaldate", release.ReleaseGroup.FirstReleaseDate, ref trackInfoUpdated, OverwriteTag);
        
        if (release.ReleaseGroup?.FirstReleaseDate?.Length >= 4)
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "originalyear", release.ReleaseGroup.FirstReleaseDate.Substring(0, 4), ref trackInfoUpdated, OverwriteTag);
        }
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Disc Number", release.Media?.FirstOrDefault()?.Position?.ToString(), ref trackInfoUpdated, OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Track Number", release.Media?.FirstOrDefault()?.Tracks?.FirstOrDefault()?.Position?.ToString(), ref trackInfoUpdated, OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Total Tracks", release.Media?.FirstOrDefault()?.TrackCount.ToString(), ref trackInfoUpdated, OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "MEDIA", release.Media?.FirstOrDefault()?.Format, ref trackInfoUpdated, OverwriteTag);

        if (trackInfoUpdated && await _mediaTagWriteService.SafeSaveAsync(track))
        {
            await _importCommandHandler.ProcessFileAsync(metadata.Path);
        }
    }
    
    public async Task<AcoustIdRecording?> GetBestMatchingAcoustIdAsync(
        AcoustIdResponse? acoustIdResponse, 
        string artist,
        string album,
        string title,
        int trackDuration,
        int matchPercentage)
    {
        if (acoustIdResponse?.Results?.Count == 0)
        {
            return null;
        }

        var highestScoreResult = acoustIdResponse
            ?.Results
            ?.Where(result => result.Recordings?.Any() == true)
            .Where(result => result.Score >= (matchPercentage / 100F))
            .OrderByDescending(result => result.Score)
            .FirstOrDefault();

        if (highestScoreResult == null)
        {
            return null;
        }

        //perhaps not the best approach but sometimes...
        bool ignoreFilters = string.IsNullOrWhiteSpace(album) ||
                             string.IsNullOrWhiteSpace(artist) ||
                             string.IsNullOrWhiteSpace(title);

        var recordingReleases = highestScoreResult.Recordings
            .Where(x => GuidHelper.GuidHasValue(x.Id))
            .Select(async x => new
            {
                RecordingId = x.Id,
                Recording = await _musicBrainzApiService.GetRecordingByIdAsync(x.Id.Value)
            })
            .Select(x => x.Result)
            .ToList();
        
        var results = highestScoreResult
            .Recordings
           ?.Select(result => new
           {
               Result = result,
               Releases = recordingReleases.FirstOrDefault(release => string.Equals(release.RecordingId, result.Id))
           })
            ?.Select(result => new
            {
                AlbumMatchedFor = result.Releases.Recording.Releases
                    .Where(release => ignoreFilters || FuzzyHelper.ExactNumberMatch(release.Title, album))
                    .Select(release => new
                    {
                        MatchedFor = FuzzyHelper.FuzzTokenSortRatioToLower(release.Title, album),
                        Release = release
                    })
                    .OrderByDescending(match => match.MatchedFor)
                    .FirstOrDefault(),
                ArtistMatchedFor = result.Result.Artists?.Sum(a => FuzzyHelper.FuzzTokenSortRatioToLower(a.Name, artist)) ?? 0,
                TitleMatchedFor = FuzzyHelper.FuzzTokenSortRatioToLower(title, result.Result.Title),
                LengthMatch = Math.Abs(trackDuration - result.Result.Duration ?? 100),
                AcoustIdResult = result
            })
            .Where(match => ignoreFilters || FuzzyHelper.ExactNumberMatch(title, match.AcoustIdResult.Result.Title))
            .Where(match => ignoreFilters || match.ArtistMatchedFor >= matchPercentage)
            .Where(match => ignoreFilters || match.TitleMatchedFor >= matchPercentage)
            .OrderByDescending(result => result.ArtistMatchedFor)
            .ThenByDescending(result => result.AlbumMatchedFor?.MatchedFor)
            .ThenByDescending(result => result.TitleMatchedFor)
            .ThenBy(result => result.LengthMatch)
            .Select(result => result)
            .ToList();

        var bestResult = results.FirstOrDefault();
        AcoustIdRecording? firstResult = bestResult?.AcoustIdResult.Result;
        if (firstResult != null)
        {
            firstResult.RecordingRelease = bestResult.AlbumMatchedFor?.Release;
            firstResult.AcoustId = highestScoreResult.Id;
        }
        return firstResult;
    }
}