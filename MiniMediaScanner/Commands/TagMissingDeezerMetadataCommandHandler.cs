using System.Diagnostics;
using ATL;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models.MusicBrainz;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;
using Newtonsoft.Json.Linq;

namespace MiniMediaScanner.Commands;

public class TagMissingDeezerMetadataCommandHandler
{
    private readonly MetadataRepository _metadataRepository;
    private readonly ArtistRepository _artistRepository;
    private readonly MediaTagWriteService _mediaTagWriteService;
    private readonly ImportCommandHandler _importCommandHandler;
    private readonly MatchRepository _matchRepository;
    private readonly DeezerRepository _deezerRepository;
    private readonly FileMetaDataService _fileMetaDataService;

    public TagMissingDeezerMetadataCommandHandler(string connectionString)
    {
        _metadataRepository = new MetadataRepository(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
        _mediaTagWriteService = new MediaTagWriteService();
        _importCommandHandler = new ImportCommandHandler(connectionString);
        _matchRepository = new MatchRepository(connectionString);
        _deezerRepository = new DeezerRepository(connectionString);
        _fileMetaDataService = new FileMetaDataService();
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
        var metadata = (await _metadataRepository.GetMissingDeezerMetadataRecordsAsync(artist))
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
        long? deezerArtistId = await _matchRepository.GetBestDeezerMatchAsync(metadata.ArtistId, metadata.Artist);
        
        if (!deezerArtistId.HasValue)
        {
            Console.WriteLine($"Nothing found for '{metadata.Album}', '{metadata.Title}'");
            return;
        }
        
        var deezerTracks = await _deezerRepository.GetTrackByArtistIdAsync(deezerArtistId.Value, metadata.Album, metadata.Title);

        deezerTracks = deezerTracks
            .Where(track => FuzzyHelper.ExactNumberMatch(metadata.Title, track.TrackName))
            .Where(track => FuzzyHelper.ExactNumberMatch(metadata.Album, track.AlbumName))
            .ToList();
        
        if (deezerTracks.Count == 0)
        {
            Console.WriteLine($"Nothing found for '{metadata.Album}', '{metadata.Title}'");
            return;
        }
        
        if (deezerTracks.Count > 1)
        {
            Console.WriteLine($"Found more then 1 Deezer track with '{metadata.Album}', '{metadata.Title}'");
            return;
        }
        
        var deezerTrack = deezerTracks.FirstOrDefault();
        var trackArtists = await _deezerRepository.GetTrackArtistsAsync(deezerTrack.TrackId, deezerArtistId.Value);
        string artists = string.Join(';', trackArtists);
        
        Console.WriteLine($"Release found for '{metadata.Path}', Title '{deezerTrack.TrackName}', Date '{deezerTrack.AlbumReleaseDate}'");

        Track track = new Track(metadata.Path);
        var metadataInfo = await _fileMetaDataService.GetMetadataInfoAsync(new FileInfo(track.Path));
        bool trackInfoUpdated = false;
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Track Id", deezerTrack.TrackId.ToString(), ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Track Explicit", deezerTrack.ExplicitLyrics ? "Y": "N", ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Track Href", deezerTrack.TrackHref, ref trackInfoUpdated, overwriteTagValue);
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Album Id", deezerTrack.AlbumId.ToString(), ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Album Href", deezerTrack.AlbumHref, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Album Release Date", deezerTrack.AlbumReleaseDate, ref trackInfoUpdated, overwriteTagValue);
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Artist Id", deezerTrack.ArtistId.ToString(), ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Artist Href", deezerTrack.ArtistHref, ref trackInfoUpdated, overwriteTagValue);
        
        if (string.IsNullOrWhiteSpace(track.Title))
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "Title", deezerTrack.TrackName, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.Album))
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "Album", deezerTrack.AlbumName, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.AlbumArtist)  || track.AlbumArtist.ToLower().Contains("various"))
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "AlbumArtist", deezerTrack.ArtistName, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.Artist) || track.Artist.ToLower().Contains("various"))
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "Artist",  deezerTrack.ArtistName, ref trackInfoUpdated, overwriteTagValue);
        }
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "ARTISTS", artists, ref trackInfoUpdated, overwriteTagValue);

        _mediaTagWriteService.UpdateTag(track, metadataInfo, "ISRC", deezerTrack.TrackISRC, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "UPC", deezerTrack.AlbumUPC, ref trackInfoUpdated, overwriteTagValue);
            
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Date", deezerTrack.AlbumReleaseDate, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Label", deezerTrack.Label, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Disc Number", deezerTrack.DiscNumber.ToString(), ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Track Number", deezerTrack.TrackPosition.ToString(), ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Total Tracks", deezerTrack.AlbumTotalTracks.ToString(), ref trackInfoUpdated, overwriteTagValue);

        if (write && trackInfoUpdated && await _mediaTagWriteService.SafeSaveAsync(track))
        {
            await _importCommandHandler.ProcessFileAsync(metadata.Path);
        }
    }
}