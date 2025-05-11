using System.Diagnostics;
using ATL;
using FuzzySharp;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models.MusicBrainz;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;
using Newtonsoft.Json.Linq;

namespace MiniMediaScanner.Commands;

public class TagMissingMetadataCommandHandler
{
    private readonly AcoustIdService _acoustIdService;
    private readonly MusicBrainzAPIService _musicBrainzAPIService;
    private readonly MetadataRepository _metadataRepository;
    private readonly ArtistRepository _artistRepository;
    private readonly MediaTagWriteService _mediaTagWriteService;
    private readonly ImportCommandHandler _importCommandHandler;
    private readonly StringNormalizerService _normalizerService;
    private readonly MusicBrainzArtistRepository _musicBrainzArtistRepository;

    public TagMissingMetadataCommandHandler(string connectionString)
    {
        _acoustIdService = new AcoustIdService();
        _musicBrainzAPIService = new MusicBrainzAPIService();
        _metadataRepository = new MetadataRepository(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
        _mediaTagWriteService = new MediaTagWriteService();
        _importCommandHandler = new ImportCommandHandler(connectionString);
        _normalizerService = new StringNormalizerService();
        _musicBrainzArtistRepository = new MusicBrainzArtistRepository(connectionString);
    }
    
    public async Task TagMetadataAsync(string accoustId, bool write, string album, bool overwriteTagValue)
    {
        await ParallelHelper.ForEachAsync(await _artistRepository.GetAllArtistNamesAsync(), 4, async artist =>
        {
            try
            {
                await TagMetadataAsync(accoustId, write, artist, album, overwriteTagValue);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        });
    }

    public async Task TagMetadataAsync(string accoustId, bool write, string artist, string album, bool overwriteTagValue)
    {
        var metadata = (await _metadataRepository.GetMissingMusicBrainzMetadataRecordsAsync(artist))
            .Where(metadata => string.IsNullOrWhiteSpace(album) || 
                               string.Equals(metadata.Album, album, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Console.WriteLine($"Checking artist '{artist}', found {metadata.Count} tracks to process");

        foreach (var record in metadata.Where(r => new FileInfo(r.Path).Exists))
        {
            try
            {
                await ProcessFileAsync(record, write, accoustId, overwriteTagValue);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
                
    private async Task ProcessFileAsync(MetadataInfo metadata, bool write, string accoustId, bool overwriteTagValue)
    {
        JObject? acoustIdLookup = await _acoustIdService.LookupAcoustIdAsync(accoustId,
            metadata.Tag_AcoustIdFingerPrint, (int)metadata.Tag_AcoustIdFingerPrint_Duration);
        
        Guid.TryParse(acoustIdLookup?["results"]?.FirstOrDefault()?["recordings"]?.FirstOrDefault()?["id"]?.ToString(), out var recordingId);
        
        var acoustId = acoustIdLookup?["results"]?.FirstOrDefault()?["id"]?.ToString();

        Track track = null;
        MusicBrainzArtistModel? artistModel = null;
        
        if (!GuidHelper.GuidHasValue(recordingId))
        {
            Console.WriteLine($"No recording ID found from AcoustID for '{metadata.Path}'");
            
            track = new Track(metadata.Path);
            recordingId = (await _musicBrainzArtistRepository.GetMusicBrainzRecordingIdByNameAsync(track.Artist, track.Album, track.Title)).Value;

            if (GuidHelper.GuidHasValue(recordingId))
            {
                artistModel = await _musicBrainzAPIService.GetRecordingByIdAsync(recordingId);
                if (artistModel != null)
                {
                    Console.WriteLine($"Found MusicBrainz data from the database, '{metadata.Path}'");
                }
            }
        }
        else
        {
            artistModel = await _musicBrainzAPIService.GetRecordingByIdAsync(recordingId);
        }

        if (track == null)
        {
            track = new Track(metadata.Path);
        }
        
        //grab the best matched release
        string? artistCountry = !string.IsNullOrWhiteSpace(track.Artist) ? await _musicBrainzArtistRepository.GetMusicBrainzArtistCountryByNameAsync(track.Artist) : string.Empty;
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
                    AlbumMatch = Fuzz.Ratio(release.Album, track.Album),
                    TitleMatch = Fuzz.Ratio(release.Title, track.Title),
                    LengthMatch = Math.Abs(track.Duration - release.Length),
                    CountryMatch = !string.IsNullOrWhiteSpace(artistCountry) ? Fuzz.Ratio(release.Release.Country, artistCountry) : 0,
                    BarcodeMatch = !string.IsNullOrWhiteSpace(release.Barcode) ? Fuzz.Ratio(release.Barcode, trackBarcode) : 0
                })
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

        if (!write)
        {
            return;
        }

        if (!GuidHelper.GuidHasValue(recordingId))
        {
            Guid.TryParse(release.Media?.FirstOrDefault()?.Tracks?.FirstOrDefault()?.Recording?.Id, out recordingId);
        }
        
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
            MusicBrainzArtistReleaseModel? withLabeLInfo = await _musicBrainzAPIService.GetReleaseWithLabelAsync(releaseId);
            var label = withLabeLInfo?.LabeLInfo?.FirstOrDefault(label => label?.Label?.Type?.ToLower().Contains("production") == true);

            if (label == null && withLabeLInfo?.LabeLInfo?.Count == 1)
            {
                label = withLabeLInfo?.LabeLInfo?.FirstOrDefault();
            }
            if (!string.IsNullOrWhiteSpace(label?.Label?.Name))
            {
                _mediaTagWriteService.UpdateTag(track, "LABEL", label?.Label.Name, ref trackInfoUpdated, overwriteTagValue);
                _mediaTagWriteService.UpdateTag(track, "CATALOGNUMBER", label?.CataLogNumber, ref trackInfoUpdated, overwriteTagValue);
            }
        }
        
        _mediaTagWriteService.UpdateTag(track, "date", release.Date, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "originaldate", release.Date, ref trackInfoUpdated, overwriteTagValue);

        if (string.IsNullOrWhiteSpace(track.Title))
        {
            _mediaTagWriteService.UpdateTag(track, "Title", release.Media?.FirstOrDefault()?.Title, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.Album))
        {
            _mediaTagWriteService.UpdateTag(track, "Album", release.Title, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.AlbumArtist)  || track.AlbumArtist.ToLower().Contains("various"))
        {
            _mediaTagWriteService.UpdateTag(track, "AlbumArtist", bestMatchedArtist?.Name, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.Artist) || track.Artist.ToLower().Contains("various"))
        {
            _mediaTagWriteService.UpdateTag(track, "Artist", bestMatchedArtist?.Name, ref trackInfoUpdated, overwriteTagValue);
        }

        _mediaTagWriteService.UpdateTag(track, "ARTISTS", artists, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "ISRC", isrcs, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "SCRIPT", release?.TextRepresentation?.Script, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "barcode", release.Barcode, ref trackInfoUpdated, overwriteTagValue);

        _mediaTagWriteService.UpdateTag(track, "MusicBrainz Artist Id", musicBrainzArtistIds, ref trackInfoUpdated, overwriteTagValue);

        if (GuidHelper.GuidHasValue(recordingId))
        {
            _mediaTagWriteService.UpdateTag(track, "MusicBrainz Track Id", recordingId.ToString(), ref trackInfoUpdated, overwriteTagValue);
        }
        
        _mediaTagWriteService.UpdateTag(track, "MusicBrainz Release Track Id", musicBrainzTrackId, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "MusicBrainz Release Artist Id", musicBrainzReleaseArtistId, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "MusicBrainz Release Group Id", musicBrainzReleaseGroupId, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "MusicBrainz Release Id", release.Id, ref trackInfoUpdated, overwriteTagValue);

        _mediaTagWriteService.UpdateTag(track, "MusicBrainz Album Artist Id", musicBrainzArtistIds, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "MusicBrainz Album Id", musicBrainzAlbumId, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "MusicBrainz Album Type", release.ReleaseGroup.PrimaryType, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "MusicBrainz Album Release Country", release.Country, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "MusicBrainz Album Status", release.Status, ref trackInfoUpdated, overwriteTagValue);

        _mediaTagWriteService.UpdateTag(track, "Acoustid Id", acoustId, ref trackInfoUpdated, overwriteTagValue);

        _mediaTagWriteService.UpdateTag(track, "Date", release.ReleaseGroup.FirstReleaseDate, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "originaldate", release.ReleaseGroup.FirstReleaseDate, ref trackInfoUpdated, overwriteTagValue);
        
        if (release.ReleaseGroup?.FirstReleaseDate?.Length >= 4)
        {
            _mediaTagWriteService.UpdateTag(track, "originalyear", release.ReleaseGroup.FirstReleaseDate.Substring(0, 4), ref trackInfoUpdated, overwriteTagValue);
        }
        
        _mediaTagWriteService.UpdateTag(track, "Disc Number", release.Media?.FirstOrDefault()?.Position?.ToString(), ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "Track Number", release.Media?.FirstOrDefault()?.Tracks?.FirstOrDefault()?.Position?.ToString(), ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "Total Tracks", release.Media?.FirstOrDefault()?.TrackCount.ToString(), ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "MEDIA", release.Media?.FirstOrDefault()?.Format, ref trackInfoUpdated, overwriteTagValue);

        if (trackInfoUpdated && await _mediaTagWriteService.SafeSaveAsync(track))
        {
            await _importCommandHandler.ProcessFileAsync(metadata.Path);
        }
    }
}