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

    public void FingerPrintMedia(string accoustId, bool write, string album)
    {
        _artistRepository.GetAllArtistNames()
            .ForEach(artist => FingerPrintMedia(accoustId, write, artist, album));
    }

    public void FingerPrintMedia(string accoustId, bool write, string artist, string album)
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
                    ProcessFile(metadata, write, accoustId);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            });
    }
                
    private void ProcessFile(MetadataInfo metadata, bool write, string accoustId)
    {
        JObject? acoustIdLookup = _acoustIdService.LookupAcoustId(accoustId,
            metadata.TagAcoustIdFingerPrint, (int)metadata.TagAcoustIdFingerPrintDuration);
        
        var recordingId = acoustIdLookup?["results"]?.FirstOrDefault()?["recordings"]?.FirstOrDefault()?["id"]?.ToString();
        var acoustId = acoustIdLookup?["results"]?.FirstOrDefault()?["id"]?.ToString();

        if (string.IsNullOrWhiteSpace(recordingId))
        {
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

        Track track = new Track(metadata.Path);

        if ((!track.Date.HasValue ||
             track.Date.Value.ToString("yyyy-MM-dd") != release.Date))
        {
            _mediaTagWriteService.UpdateTrackTag(track, "date", release.Date);
            _mediaTagWriteService.UpdateTrackTag(track, "originaldate", release.Date);
        }
        else if (release.Date.Length == 4 &&
                 (!track.Date.HasValue ||
                  track.Date.Value.Year.ToString() != release.Date))
        {
            _mediaTagWriteService.UpdateTrackTag(track, "date", release.Date);
            _mediaTagWriteService.UpdateTrackTag(track, "originaldate", release.Date);
        }


        UpdateTag(track, "barcode", release.Barcode);

        string? musicBrainzTrackId = release.Media?.FirstOrDefault()?.Tracks?.FirstOrDefault()?.Id;
        string? musicBrainzArtistId = data?.ArtistCredit?.FirstOrDefault()?.Artist?.Id;
        string? musicBrainzAlbumId = release.Id;
        string? musicBrainzReleaseGroupId = release.ReleaseGroup.Id;

        UpdateTag(track, "SCRIPT", release?.TextRepresentation?.Script);
        UpdateTag(track, "barcode", release.Barcode);

        UpdateTag(track, "MusicBrainz Artist Id", musicBrainzArtistId);
        UpdateTag(track, "MusicBrainz Track Id", musicBrainzTrackId);
        UpdateTag(track, "MusicBrainz Release Artist Id", musicBrainzArtistId);
        UpdateTag(track, "MusicBrainz Release Group Id", musicBrainzReleaseGroupId);
        UpdateTag(track, "MusicBrainz Release Id", release.Id);

        UpdateTag(track, "MusicBrainz Album Artist Id", musicBrainzArtistId);
        UpdateTag(track, "MusicBrainz Album Id", musicBrainzAlbumId);
        UpdateTag(track, "MusicBrainz Album Type", release.ReleaseGroup.PrimaryType);
        UpdateTag(track, "MusicBrainz Album Release Country", release.Country);
        UpdateTag(track, "MusicBrainz Album Status", release.Status);

        UpdateTag(track, "Acoustid Id", acoustId);

        UpdateTag(track, "Date", release.ReleaseGroup.FirstReleaseDate);
        UpdateTag(track, "originaldate", release.ReleaseGroup.FirstReleaseDate);
        UpdateTag(track, "originalyear", release.ReleaseGroup.FirstReleaseDate.Substring(0, 4));
        UpdateTag(track, "Disc Number", release.Media?.FirstOrDefault()?.Position?.ToString());
        UpdateTag(track, "Track Number", release.Media?.FirstOrDefault()?.Tracks?.FirstOrDefault()?.Position?.ToString());
        UpdateTag(track, "Total Tracks", release.Media?.FirstOrDefault()?.TrackCount.ToString());
        UpdateTag(track, "MEDIA", release.Media?.FirstOrDefault()?.Format);

        if (_mediaTagWriteService.SafeSave(track))
        {
            _importCommandHandler.ProcessFile(metadata.Path);
        }
    }

    private void UpdateTag(Track track, string tagName, string? value)
    {
        tagName = _mediaTagWriteService.GetFieldName(track, tagName);
        
        if (!string.IsNullOrWhiteSpace(value) &&
            (!track.AdditionalFields.ContainsKey(tagName) ||
             string.IsNullOrWhiteSpace(track.AdditionalFields[tagName])))
        {
            Console.WriteLine($"Updating tag '{tagName}' => '{value}'");
            _mediaTagWriteService.UpdateTrackTag(track, tagName, value);
        }
    }
}