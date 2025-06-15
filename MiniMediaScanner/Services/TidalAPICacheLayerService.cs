using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using MiniMediaScanner.Models.Tidal;

namespace MiniMediaScanner.Services;

public class TidalAPICacheLayerService
{
    private readonly TidalAPIService _tidalAPIService;
    private readonly MemoryCache _cache;
    private const int ApiDelay = 4500;
    private Stopwatch _apiStopwatch = Stopwatch.StartNew();

    public ProxyManagerService ProxyManagerService => _tidalAPIService.ProxyManagerService;
    
    public TidalAPICacheLayerService(string clientId, 
        string clientSecret, 
        string countryCode, 
        string proxyFile, 
        string singleProxy, 
        string proxyMode)
    {
        _tidalAPIService = new TidalAPIService(clientId, clientSecret, countryCode, proxyFile, singleProxy, proxyMode);
        var options = new MemoryCacheOptions();
        _cache = new MemoryCache(options);
    }
    
    public TidalAuthenticationResponse? AuthenticationResponse { get => _tidalAPIService.AuthenticationResponse; }

    private void AddToCache(string key, object? value)
    {
        if (value != null)
        {
            MemoryCacheEntryOptions options = new()
            {
                SlidingExpiration = TimeSpan.FromHours(1)
            };
            _cache.Set(key, value, options);
        }
    }

    public async Task<TidalAuthenticationResponse?> AuthenticateAsync()
    {
        return await _tidalAPIService.AuthenticateAsync();
    }

    public async Task<TidalSearchResponse?> SearchResultsArtistsAsync(string searchTerm)
    {
        string cacheKey = $"SearchResultsArtists_{searchTerm}";
        if (!_cache.TryGetValue(cacheKey, out TidalSearchResponse? result))
        {
            ApiDelaySleep();
            result = await _tidalAPIService.SearchResultsArtistsAsync(searchTerm);
            AddToCache(cacheKey, result);
        }
        return result;
    }

    public async Task<TidalSearchResponse?> GetArtistInfoByIdAsync(int artistId)
    {
        string cacheKey = $"GetArtistInfoById_{artistId}";
        if (!_cache.TryGetValue(cacheKey, out TidalSearchResponse? result))
        {
            ApiDelaySleep();
            result = await _tidalAPIService.GetArtistInfoByIdAsync(artistId);
            AddToCache(cacheKey, result);
        }
        return result;
    }

    public async Task<TidalSearchArtistNextResponse?> GetArtistNextInfoByIdAsync(int artistId, string next)
    {
        string cacheKey = $"GetArtistNextInfoById_{artistId}_{next}";
        if (!_cache.TryGetValue(cacheKey, out TidalSearchArtistNextResponse? result))
        {
            ApiDelaySleep();
            result = await _tidalAPIService.GetArtistNextInfoByIdAsync(artistId, next);
            AddToCache(cacheKey, result);
        }
        return result;
    }

    public async Task<TidalSearchResponse?> GetTracksByAlbumIdAsync(int albumId)
    {
        string cacheKey = $"GetTracksByAlbumId_{albumId}";
        if (!_cache.TryGetValue(cacheKey, out TidalSearchResponse? result))
        {
            ApiDelaySleep();
            result = await _tidalAPIService.GetTracksByAlbumIdAsync(albumId);
            AddToCache(cacheKey, result);
        }
        return result;
    }

    public async Task<TidalSearchTracksNextResponse?> GetTracksNextByAlbumIdAsync(int albumId, string next)
    {
        string cacheKey = $"GetTracksNextByAlbumId_{albumId}_{next}";
        if (!_cache.TryGetValue(cacheKey, out TidalSearchTracksNextResponse? result))
        {
            ApiDelaySleep();
            result = await _tidalAPIService.GetTracksNextByAlbumIdAsync(albumId, next);
            AddToCache(cacheKey, result);
        }
        return result;
    }

    public async Task<TidalTrackArtistResponse?> GetTrackArtistsByTrackIdAsync(int[] trackIds)
    {
        string joinedTrackIds = string.Join(",", trackIds);
        string cacheKey = $"GetTrackArtistsByTrackId_{joinedTrackIds}";
        if (!_cache.TryGetValue(cacheKey, out TidalTrackArtistResponse? result))
        {
            ApiDelaySleep();
            result = await _tidalAPIService.GetTrackArtistsByTrackIdAsync(trackIds);
            AddToCache(cacheKey, result);
        }
        return result;
    }

    private void ApiDelaySleep()
    {
        if (_apiStopwatch.ElapsedMilliseconds < ApiDelay)
        {
            Thread.Sleep(ApiDelay);
        }
        _apiStopwatch.Restart();
    }
}