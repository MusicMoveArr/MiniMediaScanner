using System.Diagnostics;
using ATL;
using MiniMediaScanner.Models;
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

    public TagMissingMetadataCommandHandler(string connectionString)
    {
        _acoustIdService = new AcoustIdService();
        _musicBrainzAPIService = new MusicBrainzAPIService();
        _metadataRepository = new MetadataRepository(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
        _mediaTagWriteService = new MediaTagWriteService();
        _importCommandHandler = new ImportCommandHandler(connectionString);
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
            metadata.TagAcoustIdFingerPrint, (int)metadata.TagAcoustIdFingerPrintDuration);
        
        var recordingId = acoustIdLookup?["results"]?.FirstOrDefault()?["recordings"]?.FirstOrDefault()?["id"]?.ToString();
        var acoustId = acoustIdLookup?["results"]?.FirstOrDefault()?["id"]?.ToString();

        if (string.IsNullOrWhiteSpace(recordingId))
        {
            Console.WriteLine($"No recording ID found from AcoustID for '{metadata.Path}'");
            return;
        }

        var data = _musicBrainzAPIService.GetRecordingById(recordingId);
        MusicBrainzArtistReleaseModel? release = data?.Releases?.FirstOrDefault();

        if (release == null)
        {
            return;
        }

        Console.WriteLine($"Release found for '{metadata.Path}', Title '{release.Title}', Date '{release.Date}', Barcode '{release.Barcode}', Country '{release.Country}'");

        if (!write)
        {
            return;
        }

        bool trackInfoUpdated = false;
        Track track = new Track(metadata.Path);

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


        UpdateTag(track, "barcode", release.Barcode, ref trackInfoUpdated, overwriteTagValue);

        string? musicBrainzTrackId = release.Media?.FirstOrDefault()?.Tracks?.FirstOrDefault()?.Id;
        string? musicBrainzArtistId = data?.ArtistCredit?.FirstOrDefault()?.Artist?.Id;
        string? musicBrainzAlbumId = release.Id;
        string? musicBrainzReleaseGroupId = release.ReleaseGroup.Id;

        UpdateTag(track, "SCRIPT", release?.TextRepresentation?.Script, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "barcode", release.Barcode, ref trackInfoUpdated, overwriteTagValue);

        UpdateTag(track, "MusicBrainz Artist Id", musicBrainzArtistId, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "MusicBrainz Track Id", recordingId, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "MusicBrainz Release Track Id", musicBrainzTrackId, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "MusicBrainz Release Artist Id", musicBrainzArtistId, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "MusicBrainz Release Group Id", musicBrainzReleaseGroupId, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "MusicBrainz Release Id", release.Id, ref trackInfoUpdated, overwriteTagValue);

        UpdateTag(track, "MusicBrainz Album Artist Id", musicBrainzArtistId, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "MusicBrainz Album Id", musicBrainzAlbumId, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "MusicBrainz Album Type", release.ReleaseGroup.PrimaryType, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "MusicBrainz Album Release Country", release.Country, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "MusicBrainz Album Status", release.Status, ref trackInfoUpdated, overwriteTagValue);

        UpdateTag(track, "Acoustid Id", acoustId, ref trackInfoUpdated, overwriteTagValue);

        UpdateTag(track, "Date", release.ReleaseGroup.FirstReleaseDate, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "originaldate", release.ReleaseGroup.FirstReleaseDate, ref trackInfoUpdated, overwriteTagValue);
        
        if (release.ReleaseGroup.FirstReleaseDate.Length >= 4)
        {
            UpdateTag(track, "originalyear", release.ReleaseGroup.FirstReleaseDate.Substring(0, 4), ref trackInfoUpdated, overwriteTagValue);
        }
        
        UpdateTag(track, "Disc Number", release.Media?.FirstOrDefault()?.Position?.ToString(), ref trackInfoUpdated, overwriteTagValue);
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
        tagName = _mediaTagWriteService.GetFieldName(track, tagName);
        
        if (!string.IsNullOrWhiteSpace(value))
        {
            if (!overwriteTagValue &&
                (track.AdditionalFields.ContainsKey(tagName) ||
                 !string.IsNullOrWhiteSpace(track.AdditionalFields[tagName])))
            {
                return;
            }
            
            bool tempIsUpdated = false;
            _mediaTagWriteService.UpdateTrackTag(track, tagName, value, ref tempIsUpdated);

            if (tempIsUpdated)
            {
                Console.WriteLine($"Updating tag '{tagName}' => '{value}'");
                trackInfoUpdated = true;
            }
        }
    }
}