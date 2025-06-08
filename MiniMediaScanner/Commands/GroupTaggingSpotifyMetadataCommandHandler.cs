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
    private readonly FileMetaDataService _fileMetaDataService;
    
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
        _fileMetaDataService = new FileMetaDataService();
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
            .Where(metadata => new FileInfo(metadata.Path).Exists)
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
        
        //match Spotify Tracks against our database
        //sort descending album's by the amount of matches
        //this method will prevent getting different AlbumId's for each track
        var matchedAlbums = spotifyTracks
            .GroupBy(track => track.AlbumId)
            .Select(tracks => new
            {
                AlbumId = tracks.Key,
                Matches = tracks.Select(track => new
                {
                    Track = track,
                    MetadataTracks = metadataModels.Select(metadata => new
                    {
                        MetadataTrack = metadata,
                        SpotifyTrack = track,
                        MatchedFor = Fuzz.Ratio(metadata.Title, track.TrackName)
                    })
                    .Where(match => match.MatchedFor >= 90)
                    .Where(match => FuzzyHelper.ExactNumberMatch(match.MetadataTrack.Title, match.SpotifyTrack.TrackName))
                    .Where(match => FuzzyHelper.ExactNumberMatch(match.MetadataTrack.AlbumName, match.SpotifyTrack.AlbumName))
                    .OrderByDescending(match => match.MatchedFor)
                    .ToList()
                        
                })
            })
            .OrderByDescending(tracks => tracks.Matches.Sum(matches => matches.MetadataTracks.Count))
            .ToList();
        
        List<Guid> processedMetadataIds = new List<Guid>();
        
        foreach(var matchedAlbum in matchedAlbums)
        {
            foreach (var matchedTrack in matchedAlbum.Matches)
            {
                foreach (var metadata in matchedTrack.MetadataTracks)
                {
                    if (processedMetadataIds.Contains(metadata.MetadataTrack.MetadataId!.Value))
                    {
                        continue;
                    }
                    
                    try
                    {
                        await ProcessFileAsync(metadata.MetadataTrack, metadata.SpotifyTrack, 
                                               overwriteTagValue, confirm, overwriteAlbumTag);
                        processedMetadataIds.Add(metadata.MetadataTrack.MetadataId!.Value);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
        }
        
        var notProcessed = metadataModels
            .Where(metadata => !processedMetadataIds.Contains(metadata.MetadataId!.Value))
            .ToList();

        foreach (var metadata in notProcessed)
        {
            //handle incorrectly tagged albums
            var foundTrack = GetSecondBestTrackMatch(allSpotifyTracks, metadata.Title, metadata.AlbumName);
            if (foundTrack == null)
            {
                Console.WriteLine($"Could not find Track title '{metadata.Title}' of album '{album}' in our Tidal database");
                continue;
            }
            
            try
            {
                await ProcessFileAsync(metadata, foundTrack, overwriteTagValue, confirm, overwriteAlbumTag);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
    
    private SpotifyTrackModel? GetSecondBestTrackMatch(List<SpotifyTrackModel> spotifyTracks, 
        string trackTitle,
        string albumTitle)
    {
        var foundTracks = spotifyTracks
            .Select(spotifyTrack => new
            {
                SpotifyTrack = spotifyTrack,
                TrackMatchedFor = Fuzz.Ratio(trackTitle, spotifyTrack.TrackName),
                AlbumMatchedFor = Fuzz.Ratio(albumTitle, spotifyTrack.AlbumName)
            })
            .Where(match => match.TrackMatchedFor >= 80)
            .Where(match => match.AlbumMatchedFor >= 80)
            .Where(match => FuzzyHelper.ExactNumberMatch(trackTitle, match.SpotifyTrack.TrackName))
            .Where(match => FuzzyHelper.ExactNumberMatch(albumTitle, match.SpotifyTrack.AlbumName))
            .OrderByDescending(match => match.TrackMatchedFor)
            .ThenByDescending(match => match.AlbumMatchedFor)
            .Select(match => match.SpotifyTrack)
            .ToList();

        return foundTracks.FirstOrDefault();
    }

    private async Task ProcessFileAsync(
        MetadataModel metadata
        , SpotifyTrackModel spotifyTrack
        , bool overwriteTagValue
        , bool autoConfirm
        , bool overwriteAlbumTag)
    {
        Track track = new Track(metadata.Path);
        var metadataInfo = _fileMetaDataService.GetMetadataInfo(track);
        
        bool trackInfoUpdated = false;
        var externalAlbumInfo = await _spotifyRepository.GetAlbumExternalValuesAsync(spotifyTrack.AlbumId);
        var externalTrackInfo = await _spotifyRepository.GetTrackExternalValuesAsync(spotifyTrack.TrackId);
        var trackArtists = await _spotifyRepository.GetTrackArtistsAsync(spotifyTrack.TrackId);

        if (string.IsNullOrWhiteSpace(track.Title) || overwriteTagValue)
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "Title", spotifyTrack.TrackName, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.Album) || overwriteAlbumTag)
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "Album", spotifyTrack.AlbumName, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.AlbumArtist)  || track.AlbumArtist.ToLower().Contains("various"))
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "AlbumArtist", spotifyTrack.ArtistName, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.Artist) || track.Artist.ToLower().Contains("various"))
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "Artist",  spotifyTrack.ArtistName, ref trackInfoUpdated, overwriteTagValue);
        }
        
        var isrcValue = externalTrackInfo.FirstOrDefault(inf => string.Equals(inf.Name, "isrc", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(isrcValue?.Value))
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "ISRC", isrcValue.Value, ref trackInfoUpdated, overwriteTagValue);
        }
        
        var upcValue = externalAlbumInfo.FirstOrDefault(inf => string.Equals(inf.Name, "upc", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(upcValue?.Value))
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "UPC", upcValue.Value, ref trackInfoUpdated, overwriteTagValue);
        }
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Date", spotifyTrack.ReleaseDate, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "LABEL", spotifyTrack.Label, ref trackInfoUpdated, overwriteTagValue);


        string artists = string.Join(';', trackArtists);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "ARTISTS", artists, ref trackInfoUpdated, overwriteTagValue);

        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Spotify Track Id", spotifyTrack.TrackId, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Spotify Track Explicit", spotifyTrack.Explicit ? "Y": "N", ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Spotify Track Uri", spotifyTrack.Uri, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Spotify Track Href", spotifyTrack.TrackHref, ref trackInfoUpdated, overwriteTagValue);
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Spotify Album Id", spotifyTrack.AlbumId, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Spotify Album Group", spotifyTrack.AlbumGroup, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Spotify Album Release Date", spotifyTrack.ReleaseDate, ref trackInfoUpdated, overwriteTagValue);
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Spotify Artist Href", spotifyTrack.ArtistHref, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Spotify Artist Genres", spotifyTrack.Genres, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Spotify Artist Id", spotifyTrack.ArtistId, ref trackInfoUpdated, overwriteTagValue);

        if (!string.IsNullOrWhiteSpace(spotifyTrack.Genres))
        {
            string genres = string.Join(";", spotifyTrack.Genres
                .Split(',')
                .Select(genre => _normalizerService.NormalizeText(genre))
                .ToList());
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "genre", genres, ref trackInfoUpdated, overwriteTagValue);
        }
        
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Disc Number", spotifyTrack.DiscNumber.ToString(), ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Track Number", spotifyTrack.TrackNumber.ToString(), ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Total Tracks", spotifyTrack.TotalTracks.ToString(), ref trackInfoUpdated, overwriteTagValue);

        if (!trackInfoUpdated)
        {
            return;
        }

        Console.WriteLine("Confirm changes? (Y/y or N/n)");
        bool confirm = autoConfirm || Console.ReadLine()?.ToLower() == "y";
        
        if (confirm && trackInfoUpdated && await _mediaTagWriteService.SafeSaveAsync(track))
        {
            await _importCommandHandler.ProcessFileAsync(metadata.Path);
        }
    }
}