using System.Xml.Schema;
using ATL;
using FuzzySharp;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models;
using MiniMediaScanner.Models.MusicBrainz;
using MiniMediaScanner.Models.Spotify;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class GroupTaggingSpotifyMetadataCommandHandler
{
    private readonly MetadataRepository _metadataRepository;
    private readonly ArtistRepository _artistRepository;
    private readonly MediaTagWriteService _mediaTagWriteService;
    private readonly ImportCommandHandler _importCommandHandler;
    private readonly StringNormalizerService _normalizerService;
    private readonly SpotifyRepository _spotifyRepository;
    private readonly MatchRepository _matchRepository;
    private readonly Dictionary<Guid, string> _metaArtistSpotify;
    
    public GroupTaggingSpotifyMetadataCommandHandler(string connectionString)
    {
        _metadataRepository = new MetadataRepository(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
        _mediaTagWriteService = new MediaTagWriteService();
        _importCommandHandler = new ImportCommandHandler(connectionString);
        _normalizerService = new StringNormalizerService();
        _spotifyRepository = new SpotifyRepository(connectionString);
        _matchRepository = new MatchRepository(connectionString);
        _metaArtistSpotify = new Dictionary<Guid, string>();
    }
    
    public async Task TagMetadataAsync(string album, bool overwriteTagValue, bool confirm, bool overwriteAlbumTag)
    {
        foreach (var artist in await _artistRepository.GetAllArtistNamesAsync())
        {
            await TagMetadataAsync(artist, album, overwriteTagValue, confirm, overwriteAlbumTag);
        }
    }

    public async Task TagMetadataAsync(string artist, string album, bool overwriteTagValue, bool confirm, bool overwriteAlbumTag)
    {
        var metadata = (await _metadataRepository.GetMetadataByArtistAsync(artist))
            .Where(metadata => string.IsNullOrWhiteSpace(album) || 
                               string.Equals(metadata.AlbumName, album, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Console.WriteLine($"Checking artist '{artist}', found {metadata.Count} tracks to process");

        foreach (var record in metadata.GroupBy(album => album.AlbumId))
        {
            try
            {
                await ProcessAlumGroupAsync(record.ToList(), artist, record.First().AlbumName!, overwriteTagValue, confirm, overwriteAlbumTag);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
                
    private async Task ProcessAlumGroupAsync(List<MetadataModel> metadataModels
        , string artist
        , string album
        , bool overwriteTagValue
        , bool confirm
        , bool overwriteAlbumTag)
    {
        Guid? artistId = metadataModels.FirstOrDefault()?.ArtistId;
        string? spotifyArtistId = string.Empty;

        if (!_metaArtistSpotify.TryGetValue(artistId.Value, out spotifyArtistId))
        {
            spotifyArtistId = await _matchRepository.GetBestSpotifyMatchAsync(artistId.Value, artist);
            _metaArtistSpotify.Add(artistId.Value, spotifyArtistId);
        }

        if (string.IsNullOrWhiteSpace(spotifyArtistId))
        {
            Console.WriteLine($"No match found for spotify metadata, artist '{artist}'");
            return;
        }
        
        var spotifyTracks = await _spotifyRepository.GetTrackByArtistIdAsync(spotifyArtistId, album, string.Empty);
        var allSpotifyTracks = await _spotifyRepository.GetTrackByArtistIdAsync(spotifyArtistId, string.Empty, string.Empty);

        if (spotifyTracks?.Count == 0)
        {
            Console.WriteLine($"For Artist '{artist}', Album '{album}' information not found in our Spotify database");
            return;
        }
        
        List<MetadataModel> missingTracks = new List<MetadataModel>();
        
        int updateSuccess = 0;
        foreach (MetadataModel metadata in metadataModels)
        {
            Track track = new Track(metadata.Path);

            SpotifyTrackModel? foundTrack = spotifyTracks
                .Select(spotifyTrack => new
                {
                    SpotifyTrack = spotifyTrack,
                    MatchedFor = Fuzz.Ratio(track.Title, spotifyTrack.TrackName)
                })
                .Where(match => match.MatchedFor >= 90)
                .OrderByDescending(match => match.MatchedFor)
                .Select(match => match.SpotifyTrack)
                .FirstOrDefault();

            if (foundTrack == null)
            {
                missingTracks.Add(metadata);
                
                //handle incorrectly tagged albums
                foundTrack = await GetSecondBestTrackMatchAsync(allSpotifyTracks, track.Title);

                if (foundTrack == null)
                {
                    Console.WriteLine($"Could not find Track title '{track.Title}' of album '{album}' in our Spotify database");
                    continue;
                }
            }

            try
            {
                await ProcessFileAsync(track, metadata, foundTrack, overwriteTagValue, confirm, overwriteAlbumTag);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            updateSuccess++;
        }
    }
    
    private async Task<SpotifyTrackModel?> GetSecondBestTrackMatchAsync(List<SpotifyTrackModel> spotifyTracks, string trackTitle)
    {
        var foundTracks = spotifyTracks
            .Select(spotifyTrack => new
            {
                SpotifyTrack = spotifyTrack,
                MatchedFor = Fuzz.Ratio(trackTitle, spotifyTrack.TrackName)
            })
            .Where(match => match.MatchedFor >= 80)
            .OrderByDescending(match => match.MatchedFor)
            .Select(match => match.SpotifyTrack)
            .ToList();

        if (foundTracks.Count >= 2)
        {
            //tough call...
            return null;
        }

        return foundTracks.FirstOrDefault();
    }

    private async Task ProcessFileAsync(Track track
        , MetadataModel metadata
        , SpotifyTrackModel spotifyTrack
        , bool overwriteTagValue
        , bool autoConfirm
        , bool overwriteAlbumTag)
    {
        bool trackInfoUpdated = false;
        var externalAlbumInfo = await _spotifyRepository.GetAlbumExternalValuesAsync(spotifyTrack.AlbumId);
        var externalTrackInfo = await _spotifyRepository.GetTrackExternalValuesAsync(spotifyTrack.TrackId);
        var trackArtists = await _spotifyRepository.GetTrackArtistsAsync(spotifyTrack.TrackId);

        if (string.IsNullOrWhiteSpace(track.Title) || overwriteTagValue)
        {
            UpdateTag(track, "Title", spotifyTrack.TrackName, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.Album) || overwriteAlbumTag)
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


        string artists = string.Join(';', trackArtists);
        UpdateTag(track, "ARTISTS", artists, ref trackInfoUpdated, overwriteTagValue);

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

        if (!string.IsNullOrWhiteSpace(spotifyTrack.Genres))
        {
            string genres = string.Join(";", spotifyTrack.Genres
                .Split(',')
                .Select(genre => _normalizerService.NormalizeText(genre))
                .ToList());
            UpdateTag(track, "genre", genres, ref trackInfoUpdated, overwriteTagValue);
        }
        
        
        UpdateTag(track, "Disc Number", spotifyTrack.DiscNumber.ToString(), ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "Track Number", spotifyTrack.TrackNumber.ToString(), ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "Total Tracks", spotifyTrack.TotalTracks.ToString(), ref trackInfoUpdated, overwriteTagValue);

        if (!trackInfoUpdated)
        {
            return;
        }

        Console.WriteLine("Confirm changes? (Y/y or N/n)");
        bool confirm = autoConfirm || Console.ReadLine()?.ToLower() == "y";
        
        Console.WriteLine("Saving");
        if (confirm && trackInfoUpdated && await _mediaTagWriteService.SafeSaveAsync(track))
        {
            Console.WriteLine("Saved");
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