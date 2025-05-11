using System.Diagnostics;
using ATL;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models.MusicBrainz;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;
using Newtonsoft.Json.Linq;

namespace MiniMediaScanner.Commands;

public class TagMissingSpotifyMetadataCommandHandler
{
    private readonly MetadataRepository _metadataRepository;
    private readonly ArtistRepository _artistRepository;
    private readonly MediaTagWriteService _mediaTagWriteService;
    private readonly ImportCommandHandler _importCommandHandler;
    private readonly StringNormalizerService _normalizerService;
    private readonly MatchRepository _matchRepository;
    private readonly SpotifyRepository _spotifyRepository;

    public TagMissingSpotifyMetadataCommandHandler(string connectionString)
    {
        _metadataRepository = new MetadataRepository(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
        _mediaTagWriteService = new MediaTagWriteService();
        _importCommandHandler = new ImportCommandHandler(connectionString);
        _normalizerService = new StringNormalizerService();
        _matchRepository = new MatchRepository(connectionString);
        _spotifyRepository = new SpotifyRepository(connectionString);
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
        var metadata = (await _metadataRepository.GetMissingSpotifyMetadataRecordsAsync(artist))
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
        string spotifyArtistId = await _matchRepository.GetBestSpotifyMatchAsync(metadata.ArtistId, metadata.Artist);
        var spotifyTracks = await _spotifyRepository.GetTrackByArtistIdAsync(spotifyArtistId, metadata.Album, metadata.Title);

        if (spotifyTracks.Count == 0)
        {
            Console.WriteLine($"Found '{metadata.Album}', '{metadata.Title}'");
            return;
        }
        if (spotifyTracks.Count > 1)
        {
            Console.WriteLine($"Found more then 1 spotify track with '{metadata.Album}', '{metadata.Title}'");
            return;
        }
        
        
        var spotifyTrack = spotifyTracks.FirstOrDefault();
        var externalAlbumInfo = await _spotifyRepository.GetAlbumExternalValuesAsync(spotifyTrack.AlbumId);
        var externalTrackInfo = await _spotifyRepository.GetTrackExternalValuesAsync(spotifyTrack.TrackId);
        var trackArtists = await _spotifyRepository.GetTrackArtistsAsync(spotifyTrack.TrackId);
        string artists = string.Join(';', trackArtists);
        
        Console.WriteLine($"Release found for '{metadata.Path}', Title '{spotifyTrack.TrackName}', Date '{spotifyTrack.ReleaseDate}'");

        Track track = new Track(metadata.Path);
        bool trackInfoUpdated = false;
        
        
        _mediaTagWriteService.UpdateTag(track, "Spotify Track Id", spotifyTrack.TrackId, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "Spotify Track Explicit", spotifyTrack.Explicit ? "Y": "N", ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "Spotify Track Uri", spotifyTrack.Uri, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "Spotify Track Href", spotifyTrack.TrackHref, ref trackInfoUpdated, overwriteTagValue);
        
        _mediaTagWriteService.UpdateTag(track, "Spotify Album Id", spotifyTrack.AlbumId, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "Spotify Album Group", spotifyTrack.AlbumGroup, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "Spotify Album Release Date", spotifyTrack.ReleaseDate, ref trackInfoUpdated, overwriteTagValue);
        
        _mediaTagWriteService.UpdateTag(track, "Spotify Artist Href", spotifyTrack.ArtistHref, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "Spotify Artist Genres", spotifyTrack.Genres, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "Spotify Artist Id", spotifyTrack.ArtistId, ref trackInfoUpdated, overwriteTagValue);
        
        if (string.IsNullOrWhiteSpace(track.Title))
        {
            _mediaTagWriteService.UpdateTag(track, "Title", spotifyTrack.TrackName, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.Album))
        {
            _mediaTagWriteService.UpdateTag(track, "Album", spotifyTrack.AlbumName, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.AlbumArtist)  || track.AlbumArtist.ToLower().Contains("various"))
        {
            _mediaTagWriteService.UpdateTag(track, "AlbumArtist", spotifyTrack.ArtistName, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.Artist) || track.Artist.ToLower().Contains("various"))
        {
            _mediaTagWriteService.UpdateTag(track, "Artist",  spotifyTrack.ArtistName, ref trackInfoUpdated, overwriteTagValue);
        }
        _mediaTagWriteService.UpdateTag(track, "ARTISTS", artists, ref trackInfoUpdated, overwriteTagValue);

        var isrcValue = externalTrackInfo.FirstOrDefault(inf => string.Equals(inf.Name, "isrc", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(isrcValue?.Value))
        {
            _mediaTagWriteService.UpdateTag(track, "ISRC", isrcValue.Value, ref trackInfoUpdated, overwriteTagValue);
        }
        
        var upcValue = externalAlbumInfo.FirstOrDefault(inf => string.Equals(inf.Name, "upc", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(upcValue?.Value))
        {
            _mediaTagWriteService.UpdateTag(track, "UPC", upcValue.Value, ref trackInfoUpdated, overwriteTagValue);
        }
        _mediaTagWriteService.UpdateTag(track, "Date", spotifyTrack.ReleaseDate, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "LABEL", spotifyTrack.Label, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "Disc Number", spotifyTrack.DiscNumber.ToString(), ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "Track Number", spotifyTrack.TrackNumber.ToString(), ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, "Total Tracks", spotifyTrack.TotalTracks.ToString(), ref trackInfoUpdated, overwriteTagValue);

        if (trackInfoUpdated && await _mediaTagWriteService.SafeSaveAsync(track))
        {
            await _importCommandHandler.ProcessFileAsync(metadata.Path);
        }
    }
}