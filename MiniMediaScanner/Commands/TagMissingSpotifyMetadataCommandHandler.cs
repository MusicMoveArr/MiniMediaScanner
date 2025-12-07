using ATL;
using Microsoft.Extensions.Caching.Memory;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models;
using MiniMediaScanner.Models.Spotify;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class TagMissingSpotifyMetadataCommandHandler
{
    private readonly MetadataRepository _metadataRepository;
    private readonly ArtistRepository _artistRepository;
    private readonly MediaTagWriteService _mediaTagWriteService;
    private readonly ImportCommandHandler _importCommandHandler;
    private readonly MatchRepository _matchRepository;
    private readonly SpotifyRepository _spotifyRepository;
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
    
    public TagMissingSpotifyMetadataCommandHandler(string connectionString)
    {
        _metadataRepository = new MetadataRepository(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
        _mediaTagWriteService = new MediaTagWriteService();
        _importCommandHandler = new ImportCommandHandler(connectionString);
        _matchRepository = new MatchRepository(connectionString);
        _spotifyRepository = new SpotifyRepository(connectionString);
        _fileMetaDataService = new FileMetaDataService();
        _asyncLock = new AsyncLock();
        var options = new MemoryCacheOptions();
        _cache = new MemoryCache(options);
        _trackScoreService = new TrackScoreService();
    }
    
    public async Task TagMetadataAsync()
    {
        await ParallelHelper.ForEachAsync(await _artistRepository.GetAllArtistNamesAsync(), 4, async artist =>
        {
            try
            {
                await TagMetadataAsync(artist, string.Empty);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        });
    }

    public async Task TagMetadataAsync(string artist, string album)
    {
        var metadata = (await _metadataRepository.GetMetadataByArtistAsync(artist))
            .Where(metadata => string.IsNullOrWhiteSpace(album) || 
                               string.Equals(metadata.AlbumName, album, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Console.WriteLine($"Checking artist '{artist}', found {metadata.Count} tracks to process");

        foreach (var record in metadata.Where(r => new FileInfo(r.Path).Exists))
        {
            try
            {
                await ProcessFileAsync(record);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
                
    private async Task ProcessFileAsync(MetadataModel metadata)
    {
        string spotifyArtistId = await _matchRepository.GetBestSpotifyMatchAsync(metadata.ArtistId.Value, metadata.ArtistName);
        
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
            Console.WriteLine($"For Artist '{metadata.ArtistName}', Album '{metadata.AlbumName}' information not found in our Spotify database");
            return;
        }
        
        var trackScoreTargetModel = new TrackScoreTargetModel
        {
            MetadataId = metadata.MetadataId!.Value,
            Artist = metadata.ArtistName ?? string.Empty,
            Album = metadata.AlbumName ?? string.Empty,
            AlbumId = metadata.AlbumId?.ToString() ?? string.Empty,
            Date = metadata.Tag_Date ?? string.Empty,
            Isrc = metadata.Tag_Isrc ?? string.Empty,
            Title = metadata.Title ?? string.Empty,
            Duration = metadata.TrackLength,
            TrackNumber = metadata.Tag_Track,
            TrackTotalCount = metadata.Tag_TrackCount,
            Upc = metadata.Tag_Upc ?? string.Empty
        };

        var spotifyScoreResult = _trackScoreService
            .GetFirstTrackScore(trackScoreTargetModel, allSpotifyTracks, 80);
        
        if (spotifyScoreResult == null)
        {
            Console.WriteLine($"Nothing found for '{metadata.AlbumName}', '{metadata.Title}'");
            return;
        }
        
        SpotifyTrackModel spotifyTrack = ((TrackScoreComparerSpotifyModel)spotifyScoreResult.TrackScoreComparer).TrackModel;
        var externalAlbumInfo = await _spotifyRepository.GetAlbumExternalValuesAsync(spotifyTrack.AlbumId);
        var externalTrackInfo = await _spotifyRepository.GetTrackExternalValuesAsync(spotifyTrack.TrackId);
        var trackArtists = await _spotifyRepository.GetTrackArtistsAsync(spotifyTrack.TrackId);
        string artists = string.Join(';', trackArtists);
        
        Console.WriteLine($"Release found for '{metadata.Path}', Title '{spotifyTrack.TrackName}', Date '{spotifyTrack.ReleaseDate}'");

        Track track = new Track(metadata.Path);
        var metadataInfo = await _fileMetaDataService.GetMetadataInfoAsync(new FileInfo(track.Path));
        bool trackInfoUpdated = false;
        
        
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
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "ARTISTS", artists, ref trackInfoUpdated, this.OverwriteTag);

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