using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using MiniMediaScanner.Models.Spotify;
using RestSharp;
using SpotifyAPI.Web;

namespace MiniMediaScanner.Services;

public class SpotifyAPICacheLayerService
{
    private int _apiDelay;
    private readonly string _spotifyClientId;
    private readonly string _spotifySecretId;
    private static SpotifyAuthenticationResponse _spotifyAuthentication;
    private Stopwatch _apiStopwatch = Stopwatch.StartNew();
    private readonly MemoryCache _cache;
    
    public SpotifyClient  SpotifyClient { get; private set; }

    public SpotifyAPICacheLayerService(
        int apiDelay,
        string spotifyClientId, 
        string spotifySecretId)
    {
        _apiDelay = apiDelay * 1000;
        _spotifyClientId = spotifyClientId;
        _spotifySecretId = spotifySecretId;
        
        var options = new MemoryCacheOptions();
        _cache = new MemoryCache(options);
    }

    public async Task<FullArtist?> GetArtistAsync(string artistId)
    {
        string cacheKey = $"GetArtist_{artistId}";
        if (!_cache.TryGetValue(cacheKey, out FullArtist? result))
        {
            ApiDelaySleep();
            result = await SpotifyClient.Artists.Get(artistId);
            AddToCache(cacheKey, result);
        }
        return result;
    }

    public async Task<FullAlbum?> GetAlbumAsync(string albumId)
    {
        string cacheKey = $"GetAlbum_{albumId}";
        if (!_cache.TryGetValue(cacheKey, out FullAlbum? result))
        {
            ApiDelaySleep();
            result = await SpotifyClient.Albums.Get(albumId);
            AddToCache(cacheKey, result);
        }
        return result;
    }
    
    private void ApiDelaySleep()
    {
        if (_apiStopwatch.ElapsedMilliseconds < _apiDelay)
        {
            Thread.Sleep(_apiDelay);
        }
        _apiStopwatch.Restart();
    }

    public async Task AuthenticateAsync()
    {
        bool newTokenApplied = false;
        if (string.IsNullOrWhiteSpace(_spotifyAuthentication?.AccessToken) ||
            (_spotifyAuthentication.ExpiresIn > 0 && DateTime.Now > _spotifyAuthentication.ExpiresAt))
        {
            _spotifyAuthentication = await GetTokenAsync();
        }

        if (SpotifyClient == null || newTokenApplied)
        {
            SpotifyClient = new SpotifyClient(_spotifyAuthentication?.AccessToken);
        }
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
    
    private async Task<SpotifyAuthenticationResponse> GetTokenAsync()
    {
        using var client = new RestClient("https://accounts.spotify.com/api/token");
        var request = new RestRequest();
        request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
        request.AddParameter("grant_type", "client_credentials");
        request.AddParameter("client_id", _spotifyClientId);
        request.AddParameter("client_secret", _spotifySecretId);
        
        var response = await client.PostAsync<SpotifyAuthenticationResponse>(request);
        response.RequestedAt = DateTime.Now;
        return response;
    }
}