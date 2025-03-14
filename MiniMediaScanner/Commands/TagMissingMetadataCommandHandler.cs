using System.Diagnostics;
using ATL;
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
    
    public void FingerPrintMedia(string accoustId, bool write, string album, bool overwriteTagValue)
    {
        _artistRepository.GetAllArtistNames()
            .ForEach(artist => FingerPrintMedia(accoustId, write, artist, album, overwriteTagValue));
    }

    public void FingerPrintMedia(string accoustId, bool write, string artist, string album, bool overwriteTagValue)
    {
        var metadata = _metadataRepository.GetMissingMusicBrainzMetadataRecords(artist)
            .Where(metadata => string.IsNullOrWhiteSpace(album) || 
                               string.Equals(metadata.Album, album, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Console.WriteLine($"Checking artist '{artist}', found {metadata.Count} tracks to process");

        metadata
            .Where(metadata => new FileInfo(metadata.Path).Exists)
            .ToList()
            .ForEach(metadata =>
            {
                try
                {
                    ProcessFile(metadata, write, accoustId, overwriteTagValue);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            });
    }
                
    private void ProcessFile(MetadataInfo metadata, bool write, string accoustId, bool overwriteTagValue)
    {
        JObject? acoustIdLookup = _acoustIdService.LookupAcoustId(accoustId,
            metadata.Tag_AcoustIdFingerPrint, (int)metadata.Tag_AcoustIdFingerPrint_Duration);
        
        Guid.TryParse(acoustIdLookup?["results"]?.FirstOrDefault()?["recordings"]?.FirstOrDefault()?["id"]?.ToString(), out var recordingId);
        
        var acoustId = acoustIdLookup?["results"]?.FirstOrDefault()?["id"]?.ToString();

        Track track = null;
        MusicBrainzArtistModel? artistModel = null;
        
        if (!GuidHelper.GuidHasValue(recordingId))
        {
            Console.WriteLine($"No recording ID found from AcoustID for '{metadata.Path}'");
            
            track = new Track(metadata.Path);
            recordingId = _musicBrainzArtistRepository.GetMusicBrainzRecordingIdByName(track.Artist, track.Album, track.Title).Value;

            if (!GuidHelper.GuidHasValue(recordingId))
            {
                artistModel = _musicBrainzAPIService.GetRecordingById(recordingId);
                if (artistModel != null)
                {
                    Console.WriteLine($"Found MusicBrainz data from the database, '{metadata.Path}'");
                }
            }
        }
        else
        {
            artistModel = _musicBrainzAPIService.GetRecordingById(recordingId);
        }
        
        MusicBrainzArtistReleaseModel? release = artistModel?.Releases?.FirstOrDefault();
        
        if (artistModel == null || release == null)
        {
            return;
        }
        
        Console.WriteLine($"Release found for '{metadata.Path}', Title '{release.Title}', Date '{release.Date}', Barcode '{release.Barcode}', Country '{release.Country}'");

        if (!write)
        {
            return;
        }

        if (track == null)
        {
            track = new Track(metadata.Path);
        }

        if (!GuidHelper.GuidHasValue(recordingId))
        {
            Guid.TryParse(release.Media?.FirstOrDefault()?.Tracks?.FirstOrDefault()?.Recording?.Id, out recordingId);
        }
        
        
        
        bool trackInfoUpdated = false;
        string? musicBrainzTrackId = release.Media?.FirstOrDefault()?.Tracks?.FirstOrDefault()?.Id;
        string? musicBrainzReleaseArtistId = artistModel?.ArtistCredit?.FirstOrDefault()?.Artist?.Id;
        string? musicBrainzAlbumId = release.Id;
        string? musicBrainzReleaseGroupId = release.ReleaseGroup.Id;
        
        string artists = string.Join(';', artistModel?.ArtistCredit.Select(artist => artist.Name));
        string musicBrainzArtistIds = string.Join(';', artistModel?.ArtistCredit.Select(artist => artist.Artist.Id));
        string isrcs = artistModel?.ISRCS != null ? string.Join(';', artistModel?.ISRCS) : string.Empty;

        if (Guid.TryParse(release.Id, out var releaseId))
        {
            MusicBrainzArtistReleaseModel? withLabeLInfo = _musicBrainzAPIService.GetReleaseWithLabel(releaseId);
            var label = withLabeLInfo?.LabeLInfo?.FirstOrDefault(label => label?.Label?.Type?.ToLower().Contains("production") == true);

            if (label == null && withLabeLInfo?.LabeLInfo?.Count == 1)
            {
                label = withLabeLInfo?.LabeLInfo?.FirstOrDefault();
            }
            if (!string.IsNullOrWhiteSpace(label?.Label?.Name))
            {
                UpdateTag(track, "LABEL", label?.Label.Name, ref trackInfoUpdated, overwriteTagValue);
                UpdateTag(track, "CATALOGNUMBER", label?.CataLogNumber, ref trackInfoUpdated, overwriteTagValue);
            }
        }
        
        if ((!track.Date.HasValue ||
             track.Date.Value.ToString("yyyy-MM-dd") != release.Date))
        {
            UpdateTag(track, "date", release.Date, ref trackInfoUpdated, overwriteTagValue);
            UpdateTag(track, "originaldate", release.Date, ref trackInfoUpdated, overwriteTagValue);
        }
        else if (release.Date.Length == 4 &&
                 (!track.Date.HasValue ||
                  track.Date.Value.Year.ToString() != release.Date))
        {
            UpdateTag(track, "date", release.Date, ref trackInfoUpdated, overwriteTagValue);
            UpdateTag(track, "originaldate", release.Date, ref trackInfoUpdated, overwriteTagValue);
        }

        if (string.IsNullOrWhiteSpace(track.Title))
        {
            UpdateTag(track, "Title", release.Media?.FirstOrDefault()?.Title, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.Album))
        {
            UpdateTag(track, "Album", release.Title, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.AlbumArtist)  || track.AlbumArtist.ToLower().Contains("various"))
        {
            UpdateTag(track, "AlbumArtist", artistModel.ArtistCredit.FirstOrDefault()?.Name, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.Artist) || track.Artist.ToLower().Contains("various"))
        {
            UpdateTag(track, "Artist", artistModel.ArtistCredit.FirstOrDefault()?.Name, ref trackInfoUpdated, overwriteTagValue);
        }

        UpdateTag(track, "ARTISTS", artists, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "ISRC", isrcs, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "SCRIPT", release?.TextRepresentation?.Script, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "barcode", release.Barcode, ref trackInfoUpdated, overwriteTagValue);

        UpdateTag(track, "MusicBrainz Artist Id", musicBrainzArtistIds, ref trackInfoUpdated, overwriteTagValue);

        if (GuidHelper.GuidHasValue(recordingId))
        {
            UpdateTag(track, "MusicBrainz Track Id", recordingId.ToString(), ref trackInfoUpdated, overwriteTagValue);
        }
        
        UpdateTag(track, "MusicBrainz Release Track Id", musicBrainzTrackId, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "MusicBrainz Release Artist Id", musicBrainzReleaseArtistId, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "MusicBrainz Release Group Id", musicBrainzReleaseGroupId, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "MusicBrainz Release Id", release.Id, ref trackInfoUpdated, overwriteTagValue);

        UpdateTag(track, "MusicBrainz Album Artist Id", musicBrainzArtistIds, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "MusicBrainz Album Id", musicBrainzAlbumId, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "MusicBrainz Album Type", release.ReleaseGroup.PrimaryType, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "MusicBrainz Album Release Country", release.Country, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "MusicBrainz Album Status", release.Status, ref trackInfoUpdated, overwriteTagValue);

        UpdateTag(track, "Acoustid Id", acoustId, ref trackInfoUpdated, overwriteTagValue);

        UpdateTag(track, "Date", release.ReleaseGroup.FirstReleaseDate, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "originaldate", release.ReleaseGroup.FirstReleaseDate, ref trackInfoUpdated, overwriteTagValue);
        
        if (release.ReleaseGroup?.FirstReleaseDate?.Length >= 4)
        {
            UpdateTag(track, "originalyear", release.ReleaseGroup.FirstReleaseDate.Substring(0, 4), ref trackInfoUpdated, overwriteTagValue);
        }
        
        //UpdateTag(track, "Disc Number", release.Media?.FirstOrDefault()?.Position?.ToString(), ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "Track Number", release.Media?.FirstOrDefault()?.Tracks?.FirstOrDefault()?.Position?.ToString(), ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "Total Tracks", release.Media?.FirstOrDefault()?.TrackCount.ToString(), ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "MEDIA", release.Media?.FirstOrDefault()?.Format, ref trackInfoUpdated, overwriteTagValue);

        if (trackInfoUpdated && _mediaTagWriteService.SafeSave(track))
        {
            _importCommandHandler.ProcessFile(metadata.Path);
        }
    }

    private void UpdateTag(Track track, string tagName, string? value, ref bool trackInfoUpdated, bool overwriteTagValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (int.TryParse(value, out int intValue) && intValue == 0)
        {
            return;
        }
        
        tagName = _mediaTagWriteService.GetFieldName(track, tagName);
        value = _normalizerService.ReplaceInvalidCharacters(value);
        
        if (!overwriteTagValue &&
            (track.AdditionalFields.ContainsKey(tagName) ||
             !string.IsNullOrWhiteSpace(track.AdditionalFields[tagName])))
        {
            return;
        }
        
        string orgValue = string.Empty;
        bool tempIsUpdated = false;
        _mediaTagWriteService.UpdateTrackTag(track, tagName, value, ref tempIsUpdated, ref orgValue);

        if (tempIsUpdated)
        {
            Console.WriteLine($"Updating tag '{tagName}' value '{orgValue}' =>  '{value}'");
            trackInfoUpdated = true;
        }
    }
}