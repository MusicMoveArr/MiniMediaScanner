using Microsoft.Extensions.Caching.Memory;
using MiniMediaScanner.Models.Tidal;

namespace MiniMediaScanner.Services;

public class TidalAPICacheLayerService
{
    private readonly TidalAPIService _tidalAPIService;
    private readonly MemoryCache _cache;

    public ProxyManagerService ProxyManagerService => _tidalAPIService.ProxyManagerService;
    
    public TidalAPICacheLayerService(
        List<TidalTokenClientSecret> secretTokens,
        string countryCode, 
        string proxyFile, 
        string singleProxy, 
        string proxyMode)
    {
        _tidalAPIService = new TidalAPIService(secretTokens, countryCode, proxyFile, singleProxy, proxyMode);
        var options = new MemoryCacheOptions();
        _cache = new MemoryCache(options);
    }
    
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

    public async Task<TidalAuthenticationResponse?> AuthenticateAsync(TidalTokenClientSecret secretToken)
    {
        return await _tidalAPIService.AuthenticateAsync(secretToken);
    }

    public async Task<TidalSearchResponse?> SearchResultsArtistsAsync(string searchTerm)
    {
        string cacheKey = $"SearchResultsArtists_{searchTerm}";
        if (!_cache.TryGetValue(cacheKey, out TidalSearchResponse? result))
        {
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
            result = await _tidalAPIService.GetTracksNextByAlbumIdAsync(albumId, next);
            AddToCache(cacheKey, result);
        }
        return result;
    }

    public async Task<TidalTrackArtistResponse?> GetSimilarTracksByTrackIdAsync(int trackId)
    {
        string cacheKey = $"GetSimilarTracksByTrackId_{trackId}";
        if (!_cache.TryGetValue(cacheKey, out TidalTrackArtistResponse? result))
        {
            result = await _tidalAPIService.GetSimilarTracksByTrackIdAsync(trackId);
            AddToCache(cacheKey, result);
        }
        return result;
    }

    public async Task<TidalTrackArtistResponse?> GetSimilarArtistsByArtistIdAsync(int artistId)
    {
        string cacheKey = $"GetSimilarArtistsByArtistId_{artistId}";
        if (!_cache.TryGetValue(cacheKey, out TidalTrackArtistResponse? result))
        {
            result = await _tidalAPIService.GetSimilarArtistsByArtistIdAsync(artistId);
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
            result = await _tidalAPIService.GetTrackArtistsByTrackIdAsync(trackIds);
            AddToCache(cacheKey, result);
        }
        return result;
    }

}