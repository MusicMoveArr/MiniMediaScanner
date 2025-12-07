using System.Xml.Schema;
using ATL;
using FuzzySharp;
using Microsoft.Extensions.Caching.Memory;
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
    private readonly AsyncLock _asyncLock;
    private readonly MemoryCache _cache;
    private readonly TrackScoreService _trackScoreService;
    
    public bool Confirm { get; set; }
    public bool OverwriteTag { get; set; }
    public bool OverwriteArtist { get; set; }
    public bool OverwriteAlbumArtist { get; set; }
    public bool OverwriteAlbum { get; set; }
    public bool OverwriteTrack { get; set; }
    
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
        _asyncLock = new AsyncLock();
        var options = new MemoryCacheOptions();
        _cache = new MemoryCache(options);
        _trackScoreService = new TrackScoreService();
    }
    
    public async Task TagMetadataAsync()
    {
        foreach (var artist in await _artistRepository.GetAllArtistNamesAsync())
        {
            await TagMetadataAsync(artist, string.Empty);
        }
    }

    public async Task TagMetadataAsync(string artist, string album)
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
                await ProcessAlumGroupAsync(
                    record.ToList(), 
                    artist, 
                    record.First().AlbumName!);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
                
    private async Task ProcessAlumGroupAsync(List<MetadataModel> metadataModels
        , string artist
        , string album)
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

        var allSpotifyTracks = new List<TrackScoreComparerSpotifyModel>();
        string spotifyArtistTracksKey = $"SpotifyArtistTracks_{spotifyArtistId}";
        
        using (await _asyncLock.LockAsync())
        {
            if (!_cache.TryGetValue(spotifyArtistTracksKey, out List<TrackScoreComparerSpotifyModel>? result))
            {
                var tempSpotifyTracks = await _spotifyRepository.GetTrackByArtistIdAsync(spotifyArtistId, string.Empty, string.Empty);
                allSpotifyTracks.AddRange(tempSpotifyTracks
                    .Select(model => new TrackScoreComparerSpotifyModel(model)));
                
                MemoryCacheEntryOptions options = new()
                {
                    SlidingExpiration = TimeSpan.FromHours(1)
                };
                
                _cache.Set(spotifyArtistTracksKey, allSpotifyTracks, options);
            }
            else
            {
                allSpotifyTracks = result;
            }
        }
        
        if (allSpotifyTracks?.Count == 0)
        {
            Console.WriteLine($"For Artist '{artist}', Album '{album}' information not found in our Spotify database");
            return;
        }
        
        var trackScoreTargetModels = metadataModels
            .Select(track => new TrackScoreTargetModel
            {
                MetadataId = track.MetadataId!.Value,
                Artist = track.ArtistName ?? string.Empty,
                Album = track.AlbumName ?? string.Empty,
                AlbumId = track.AlbumId?.ToString() ?? string.Empty,
                Date = track.Tag_Date ?? string.Empty,
                Isrc = track.Tag_Isrc ?? string.Empty,
                Title = track.Title ?? string.Empty,
                Duration = track.TrackLength,
                TrackNumber = track.Tag_Track,
                TrackTotalCount = track.Tag_TrackCount,
                Upc = track.Tag_Upc ?? string.Empty
            })
            .ToList();

        var trackList = _trackScoreService
            .GetAllTrackScore(trackScoreTargetModels, allSpotifyTracks, 80)
            .GroupBy(tracks => tracks.TrackScoreComparer.AlbumId)
            .OrderByDescending(tracks => tracks.Count());
        
        
        List<Guid> processedMetadataIds = new List<Guid>();
        foreach (var groupedTracks in trackList)
        {
            foreach (var matchedTrack in groupedTracks)
            {
                if (processedMetadataIds.Contains(matchedTrack.TrackScore.MetadataId))
                {
                    continue;
                }

                var metadataModel = metadataModels
                    .FirstOrDefault(m => m.MetadataId == matchedTrack.TrackScore.MetadataId);
                
                try
                {
                    SpotifyTrackModel spotifyTrackModel = ((TrackScoreComparerSpotifyModel)matchedTrack.TrackScoreComparer).TrackModel;
                    
                    Console.WriteLine($"Processing file '{metadataModel.Path}'");
                    await ProcessFileAsync(metadataModel, spotifyTrackModel);
                    processedMetadataIds.Add(metadataModel.MetadataId.Value);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
    }

    private async Task ProcessFileAsync(
        MetadataModel metadata
        , SpotifyTrackModel spotifyTrack)
    {
        Track track = new Track(metadata.Path);
        var metadataInfo = _fileMetaDataService.GetMetadataInfo(track);
        
        bool trackInfoUpdated = false;
        var externalAlbumInfo = await _spotifyRepository.GetAlbumExternalValuesAsync(spotifyTrack.AlbumId);
        var externalTrackInfo = await _spotifyRepository.GetTrackExternalValuesAsync(spotifyTrack.TrackId);
        var trackArtists = await _spotifyRepository.GetTrackArtistsAsync(spotifyTrack.TrackId);

        if (string.IsNullOrWhiteSpace(track.Title) || this.OverwriteTrack)
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "Title", spotifyTrack.TrackName, ref trackInfoUpdated, this.OverwriteTag);
        }
        if (string.IsNullOrWhiteSpace(track.Album) || this.OverwriteAlbum)
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "Album", spotifyTrack.AlbumName, ref trackInfoUpdated, this.OverwriteTag);
        }
        if (string.IsNullOrWhiteSpace(track.AlbumArtist)  || OverwriteAlbumArtist)
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "AlbumArtist", spotifyTrack.ArtistName, ref trackInfoUpdated, this.OverwriteTag);
        }
        if (string.IsNullOrWhiteSpace(track.Artist) || OverwriteArtist)
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "Artist",  spotifyTrack.ArtistName, ref trackInfoUpdated, this.OverwriteTag);
        }
        
        var isrcValue = externalTrackInfo.FirstOrDefault(inf => string.Equals(inf.Name, "isrc", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(isrcValue?.Value))
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "ISRC", isrcValue.Value, ref trackInfoUpdated, this.OverwriteTag);
        }
        
        var upcValue = externalAlbumInfo.FirstOrDefault(inf => string.Equals(inf.Name, "upc", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(upcValue?.Value))
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "UPC", upcValue.Value, ref trackInfoUpdated, this.OverwriteTag);
        }
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Date", spotifyTrack.ReleaseDate, ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "LABEL", spotifyTrack.Label, ref trackInfoUpdated, this.OverwriteTag);


        string artists = string.Join(';', trackArtists);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "ARTISTS", artists, ref trackInfoUpdated, this.OverwriteTag);

        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Spotify Track Id", spotifyTrack.TrackId, ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Spotify Track Explicit", spotifyTrack.Explicit ? "Y": "N", ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Spotify Track Uri", spotifyTrack.Uri, ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Spotify Track Href", spotifyTrack.TrackHref, ref trackInfoUpdated, this.OverwriteTag);
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Spotify Album Id", spotifyTrack.AlbumId, ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Spotify Album Group", spotifyTrack.AlbumGroup, ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Spotify Album Release Date", spotifyTrack.ReleaseDate, ref trackInfoUpdated, this.OverwriteTag);
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Spotify Artist Href", spotifyTrack.ArtistHref, ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Spotify Artist Genres", spotifyTrack.Genres, ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Spotify Artist Id", spotifyTrack.ArtistId, ref trackInfoUpdated, this.OverwriteTag);

        if (!string.IsNullOrWhiteSpace(spotifyTrack.Genres))
        {
            string genres = string.Join(";", spotifyTrack.Genres
                .Split(',')
                .Select(genre => _normalizerService.NormalizeText(genre))
                .ToList());
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "genre", genres, ref trackInfoUpdated, this.OverwriteTag);
        }
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Disc Number", spotifyTrack.DiscNumber.ToString(), ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Track Number", spotifyTrack.TrackNumber.ToString(), ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Total Tracks", spotifyTrack.TotalTracks.ToString(), ref trackInfoUpdated, this.OverwriteTag);

        if (!trackInfoUpdated)
        {
            return;
        }

        Console.WriteLine("Confirm changes? (Y/y or N/n)");
        bool confirm = this.Confirm || Console.ReadLine()?.ToLower() == "y";
        
        if (confirm && trackInfoUpdated && await _mediaTagWriteService.SafeSaveAsync(track))
        {
            await _importCommandHandler.ProcessFileAsync(metadata.Path);
        }
    }
}