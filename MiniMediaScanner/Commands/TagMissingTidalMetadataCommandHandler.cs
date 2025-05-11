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

        tidalTracks = tidalTracks
            .Where(track => FuzzyHelper.ExactNumberMatch(metadata.Title, track.TrackName))
            .Where(track => FuzzyHelper.ExactNumberMatch(metadata.Album, track.AlbumName))
            .ToList();
        
        if (tidalTracks.Count == 0)
        {
            Console.WriteLine($"Nothing found for '{metadata.Album}', '{metadata.Title}'");
            return;
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
        
        _mediaTagWriteService.UpdateTag(track, "Tidal Track Id", tidalTrack.TrackId.ToString(), ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "Tidal Track Explicit", tidalTrack.Explicit ? "Y": "N", ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "Tidal Track Href", tidalTrack.TrackHref, ref trackInfoUpdated, overwriteTagValue);
        
        _mediaTagWriteService.UpdateTag(track, "Tidal Album Id", tidalTrack.AlbumId.ToString(), ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "Tidal Album Href", tidalTrack.AlbumHref, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "Tidal Album Release Date", tidalTrack.ReleaseDate, ref trackInfoUpdated, overwriteTagValue);
        
        _mediaTagWriteService.UpdateTag(track, "Tidal Artist Id", tidalTrack.ArtistId.ToString(), ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "Tidal Artist Href", tidalTrack.ArtistHref, ref trackInfoUpdated, overwriteTagValue);
        
        if (string.IsNullOrWhiteSpace(track.Title))
        {
            _mediaTagWriteService.UpdateTag(track, "Title", tidalTrack.FullTrackName, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.Album))
        {
            _mediaTagWriteService.UpdateTag(track, "Album", tidalTrack.AlbumName, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.AlbumArtist)  || track.AlbumArtist.ToLower().Contains("various"))
        {
            _mediaTagWriteService.UpdateTag(track, "AlbumArtist", tidalTrack.ArtistName, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.Artist) || track.Artist.ToLower().Contains("various"))
        {
            _mediaTagWriteService.UpdateTag(track, "Artist",  tidalTrack.ArtistName, ref trackInfoUpdated, overwriteTagValue);
        }
        _mediaTagWriteService.UpdateTag(track, "ARTISTS", artists, ref trackInfoUpdated, overwriteTagValue);

        _mediaTagWriteService.UpdateTag(track, "ISRC", tidalTrack.TrackISRC, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "UPC", tidalTrack.AlbumUPC, ref trackInfoUpdated, overwriteTagValue);
            
        _mediaTagWriteService.UpdateTag(track, "Date", tidalTrack.ReleaseDate, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "Copyright", tidalTrack.Copyright, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "Disc Number", tidalTrack.DiscNumber.ToString(), ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "Track Number", tidalTrack.TrackNumber.ToString(), ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "Total Tracks", tidalTrack.TotalTracks.ToString(), ref trackInfoUpdated, overwriteTagValue);

        if (trackInfoUpdated && await _mediaTagWriteService.SafeSaveAsync(track))
        {
            await _importCommandHandler.ProcessFileAsync(metadata.Path);
        }
    }
}