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
    
    public void TagMetadata(bool write, string album, bool overwriteTagValue)
    {
        _artistRepository.GetAllArtistNames()
            .ForEach(artist => TagMetadata(write, artist, album, overwriteTagValue));
    }

    public void TagMetadata(bool write, string artist, string album, bool overwriteTagValue)
    {
        var metadata = _metadataRepository.GetMissingSpotifyMetadataRecords(artist)
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
                    ProcessFile(metadata, write, overwriteTagValue);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            });
    }
                
    private void ProcessFile(MetadataInfo metadata, bool write, bool overwriteTagValue)
    {
        string spotifyArtistId = _matchRepository.GetBestSpotifyMatch(metadata.ArtistId, metadata.Artist);
        var spotifyTracks = _spotifyRepository.GetTrackByArtistId(spotifyArtistId, metadata.Album, metadata.Title);

        if (spotifyTracks.Count == 0)
        {
            Console.WriteLine($"Found No  '{metadata.Album}', '{metadata.Title}'");
            return;
        }
        if (spotifyTracks.Count > 1)
        {
            Console.WriteLine($"Found more then 1 spotify track with '{metadata.Album}', '{metadata.Title}'");
            return;
        }
        
        
        var spotifyTrack = spotifyTracks.FirstOrDefault();
        var externalAlbumInfo = _spotifyRepository.GetAlbumExternalValues(spotifyTrack.AlbumId);
        var externalTrackInfo = _spotifyRepository.GetTrackExternalValues(spotifyTrack.TrackId);
        var trackArtists = _spotifyRepository.GetTrackArtists(spotifyTrack.TrackId);
        string artists = string.Join(';', trackArtists);
        
        Console.WriteLine($"Release found for '{metadata.Path}', Title '{spotifyTrack.TrackName}', Date '{spotifyTrack.ReleaseDate}'");

        Track track = new Track(metadata.Path);
        bool trackInfoUpdated = false;
        
        
        UpdateTag(track, "Spotify Track Id", spotifyTrack.TrackId, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "Spotify Track Explicit", spotifyTrack.Explicit ? "Y": "N", ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "Spotify Track Uri", spotifyTrack.Uri, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "Spotify Track Href", spotifyTrack.TrackHref, ref trackInfoUpdated, overwriteTagValue);
        
        UpdateTag(track, "Spotify Album Id", spotifyTrack.AlbumId, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "Spotify Album Group", spotifyTrack.AlbumGroup, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "Spotify Album Release Date", spotifyTrack.ReleaseDate, ref trackInfoUpdated, overwriteTagValue);
        
        UpdateTag(track, "Spotify Artist Href", spotifyTrack.ArtistHref, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "Spotify Artist Genres", spotifyTrack.Genres, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "Spotify Artist Id", spotifyTrack.ArtistId, ref trackInfoUpdated, overwriteTagValue);
        
        if (string.IsNullOrWhiteSpace(track.Title))
        {
            UpdateTag(track, "Title", spotifyTrack.TrackName, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.Album))
        {
            UpdateTag(track, "Album", spotifyTrack.AlbumName, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.AlbumArtist)  || track.AlbumArtist.ToLower().Contains("various"))
        {
            UpdateTag(track, "AlbumArtist", spotifyTrack.ArtistName, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.Artist) || track.Artist.ToLower().Contains("various"))
        {
            UpdateTag(track, "Artist",  spotifyTrack.ArtistName, ref trackInfoUpdated, overwriteTagValue);
        }
        UpdateTag(track, "ARTISTS", artists, ref trackInfoUpdated, overwriteTagValue);

        var isrcValue = externalTrackInfo.FirstOrDefault(inf => string.Equals(inf.Name, "isrc", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(isrcValue?.Value))
        {
            UpdateTag(track, "ISRC", isrcValue.Value, ref trackInfoUpdated, overwriteTagValue);
        }
        
        var upcValue = externalAlbumInfo.FirstOrDefault(inf => string.Equals(inf.Name, "upc", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(upcValue?.Value))
        {
            UpdateTag(track, "UPC", upcValue.Value, ref trackInfoUpdated, overwriteTagValue);
        }
        UpdateTag(track, "Date", spotifyTrack.ReleaseDate, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "LABEL", spotifyTrack.Label, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "Disc Number", spotifyTrack.DiscNumber.ToString(), ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "Track Number", spotifyTrack.TrackNumber.ToString(), ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "Total Tracks", spotifyTrack.TotalTracks.ToString(), ref trackInfoUpdated, overwriteTagValue);

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