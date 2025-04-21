using System.Diagnostics;
using ATL;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models.MusicBrainz;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;
using Newtonsoft.Json.Linq;

namespace MiniMediaScanner.Commands;

public class TagMissingTidalMetadataCommandHandler
{
    private readonly MetadataRepository _metadataRepository;
    private readonly ArtistRepository _artistRepository;
    private readonly MediaTagWriteService _mediaTagWriteService;
    private readonly ImportCommandHandler _importCommandHandler;
    private readonly StringNormalizerService _normalizerService;
    private readonly MatchRepository _matchRepository;
    private readonly TidalRepository _tidalRepository;

    public TagMissingTidalMetadataCommandHandler(string connectionString)
    {
        _metadataRepository = new MetadataRepository(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
        _mediaTagWriteService = new MediaTagWriteService();
        _importCommandHandler = new ImportCommandHandler(connectionString);
        _normalizerService = new StringNormalizerService();
        _matchRepository = new MatchRepository(connectionString);
        _tidalRepository = new TidalRepository(connectionString);
    }
    
    public async Task TagMetadataAsync(bool write, string album, bool overwriteTagValue)
    {
        await ParallelHelper.ForEachAsync(await _artistRepository.GetAllArtistNamesAsync(), 4, async artist =>
        {
            try
            {
                await TagMetadataAsync(write, artist, album, overwriteTagValue);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        });
    }

    public async Task TagMetadataAsync(bool write, string artist, string album, bool overwriteTagValue)
    {
        var metadata = (await _metadataRepository.GetMissingTidalMetadataRecordsAsync(artist))
            .Where(metadata => string.IsNullOrWhiteSpace(album) || 
                               string.Equals(metadata.Album, album, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Console.WriteLine($"Checking artist '{artist}', found {metadata.Count} tracks to process");

        foreach (var record in metadata.Where(r => new FileInfo(r.Path).Exists))
        {
            try
            {
                await ProcessFileAsync(record, write, overwriteTagValue);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
                
    private async Task ProcessFileAsync(MetadataInfo metadata, bool write, bool overwriteTagValue)
    {
        int? tidalArtistId = await _matchRepository.GetBestTidalMatchAsync(metadata.ArtistId, metadata.Artist);
        
        if (!tidalArtistId.HasValue)
        {
            Console.WriteLine($"Nothing found for '{metadata.Album}', '{metadata.Title}'");
            return;
        }
        
        var tidalTracks = await _tidalRepository.GetTrackByArtistIdAsync(tidalArtistId.Value, metadata.Album, metadata.Title);

        if (tidalTracks.Count == 0)
        {
            Console.WriteLine($"Nothing found for '{metadata.Album}', '{metadata.Title}'");
            return;
        }

        if (tidalTracks.Count > 1)
        {
            tidalTracks = tidalTracks
                .Where(tidalTrack => FuzzyHelper.ExactNumberMatch(metadata.Album, tidalTrack.AlbumName))
                .Where(tidalTrack => FuzzyHelper.ExactNumberMatch(metadata.Title, tidalTrack.TrackName))
                .Where(tidalTrack => FuzzyHelper.ExactNumberMatch(metadata.Tag_Length, tidalTrack.Duration))
                .ToList();

            if (tidalTracks.Count == 0)
            {
                Console.WriteLine($"Nothing found for '{metadata.Album}', '{metadata.Title}'");
                return;
            }
        }
        
        if (tidalTracks.Count > 1)
        {
            Console.WriteLine($"Found more then 1 spotify track with '{metadata.Album}', '{metadata.Title}'");
            return;
        }
        
        var tidalTrack = tidalTracks.FirstOrDefault();
        var trackArtists = await _tidalRepository.GetTrackArtistsAsync(tidalTrack.TrackId, tidalArtistId.Value);
        string artists = string.Join(';', trackArtists);
        
        Console.WriteLine($"Release found for '{metadata.Path}', Title '{tidalTrack.TrackName}', Date '{tidalTrack.ReleaseDate}'");

        Track track = new Track(metadata.Path);
        bool trackInfoUpdated = false;
        
        UpdateTag(track, "Tidal Track Id", tidalTrack.TrackId.ToString(), ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "Tidal Track Explicit", tidalTrack.Explicit ? "Y": "N", ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "Tidal Track Href", tidalTrack.TrackHref, ref trackInfoUpdated, overwriteTagValue);
        
        UpdateTag(track, "Tidal Album Id", tidalTrack.AlbumId.ToString(), ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "Tidal Album Href", tidalTrack.AlbumHref, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "Tidal Album Release Date", tidalTrack.ReleaseDate, ref trackInfoUpdated, overwriteTagValue);
        
        UpdateTag(track, "Tidal Artist Id", tidalTrack.ArtistId.ToString(), ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "Tidal Artist Href", tidalTrack.ArtistHref, ref trackInfoUpdated, overwriteTagValue);
        
        if (string.IsNullOrWhiteSpace(track.Title))
        {
            UpdateTag(track, "Title", tidalTrack.TrackName, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.Album))
        {
            UpdateTag(track, "Album", tidalTrack.AlbumName, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.AlbumArtist)  || track.AlbumArtist.ToLower().Contains("various"))
        {
            UpdateTag(track, "AlbumArtist", tidalTrack.ArtistName, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.Artist) || track.Artist.ToLower().Contains("various"))
        {
            UpdateTag(track, "Artist",  tidalTrack.ArtistName, ref trackInfoUpdated, overwriteTagValue);
        }
        UpdateTag(track, "ARTISTS", artists, ref trackInfoUpdated, overwriteTagValue);

        UpdateTag(track, "ISRC", tidalTrack.TrackISRC, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "UPC", tidalTrack.AlbumUPC, ref trackInfoUpdated, overwriteTagValue);
            
        UpdateTag(track, "Date", tidalTrack.ReleaseDate, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "Copyright", tidalTrack.Copyright, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "Disc Number", tidalTrack.DiscNumber.ToString(), ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "Track Number", tidalTrack.TrackNumber.ToString(), ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "Total Tracks", tidalTrack.TotalTracks.ToString(), ref trackInfoUpdated, overwriteTagValue);

        if (trackInfoUpdated && await _mediaTagWriteService.SafeSaveAsync(track))
        {
            await _importCommandHandler.ProcessFileAsync(metadata.Path);
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