using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using MiniMediaScanner.Models.Spotify;
using RestSharp;
using SpotifyAPI.Web;

namespace MiniMediaScanner.Services;

public class SpotifyAPICacheLayerService
{
    private int _apiDelay;
    private readonly List<string> _spotifyClientIds;
    private readonly List<string> _spotifySecretIds;
    private static List<SpotifyTokenClientSecret> _secretTokens;
    private readonly MemoryCache _cache;
    
    public SpotifyAPICacheLayerService(
        int apiDelay,
        List<SpotifyTokenClientSecret> secretTokens)
    {
        _apiDelay = apiDelay * 1000;
        _secretTokens =  secretTokens;
        
        var options = new MemoryCacheOptions();
        _cache = new MemoryCache(options);
    }

    public async Task<FullArtist?> GetArtistAsync(string artistId)
    {
        
        string cacheKey = $"GetArtist_{artistId}";
        if (!_cache.TryGetValue(cacheKey, out FullArtist? result))
        {
            result = await TrySpotifyRequestAsync<FullArtist>(async secretToken => 
                await secretToken?.SpotifyClient?.Artists?.Get(artistId));
            
            AddToCache(cacheKey, result);
        }
        
        return result;
    }

    public async Task<FullAlbum?> GetAlbumAsync(string albumId)
    {
        string cacheKey = $"GetAlbum_{albumId}";
        if (!_cache.TryGetValue(cacheKey, out FullAlbum? result))
        {
            result = await TrySpotifyRequestAsync<FullAlbum>(async secretToken => 
                await secretToken?.SpotifyClient?.Albums?.Get(albumId));

            AddToCache(cacheKey, result);
        }
        return result;
    }

    public async Task<T?> TrySpotifyRequestAsync<T>(Func<SpotifyTokenClientSecret, Task<T>> action)
    {
        T result =  default(T);
        while (true)
        {
            SpotifyTokenClientSecret? secretToken = await GetNextTokenSecretAsync();

            if (secretToken == null)
            {
                break;
            }

            try
            {
                result = await action(secretToken);
                break;
            }
            catch (APITooManyRequestsException ex)
            {
                secretToken.TooManyRequestsTimeout = DateTime.Now.Add(ex.RetryAfter.Add(TimeSpan.FromSeconds(_apiDelay)));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"error from TrySpotifyRequestAsync, {ex.Message}, {ex.StackTrace}");
                throw;
            }
        }

        return result;
    }

    public async Task AuthenticateAsync(SpotifyTokenClientSecret secretToken)
    {
        bool newTokenApplied = false;
        if (string.IsNullOrWhiteSpace(secretToken.AuthenticationResponse?.AccessToken) ||
            (secretToken.AuthenticationResponse?.ExpiresIn > 0 && DateTime.Now > secretToken?.AuthenticationResponse?.ExpiresAt))
        {
            SpotifyAuthenticationResponse authResponse = await GetTokenAsync(secretToken);
            secretToken.AuthenticationResponse = authResponse;
            secretToken.SpotifyClient = new SpotifyClient(authResponse.AccessToken);
        }
    }
    
    private async Task<SpotifyAuthenticationResponse> GetTokenAsync(SpotifyTokenClientSecret secretToken)
    {
        using var client = new RestClient("https://accounts.spotify.com/api/token");
        var request = new RestRequest();
        request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
        request.AddParameter("grant_type", "client_credentials");
        request.AddParameter("client_id", secretToken.ClientId);
        request.AddParameter("client_secret", secretToken.SecretId);
        
        var response = await client.PostAsync<SpotifyAuthenticationResponse>(request);
        if (response != null)
        {
            response.RequestedAt = DateTime.Now;
        }
        return response;
    }
    
    public async Task<SpotifyTokenClientSecret?> GetNextTokenSecretAsync()
    {
        //get secret token that was used >ApiDelay time
        SpotifyTokenClientSecret? nextSecretToken = _secretTokens
            .Where(token => token.AuthenticationResponse != null)
            .Where(token => !token.TooManyRequestsTimeout.HasValue || DateTime.Now > token.TooManyRequestsTimeout)
            .FirstOrDefault(token => token.LastUsedTime.ElapsedMilliseconds > _apiDelay);

        //authenticate another secret token
        if (nextSecretToken == null)
        {
            nextSecretToken = _secretTokens
                .Where(token => !token.TooManyRequestsTimeout.HasValue || DateTime.Now > token.TooManyRequestsTimeout)
                .FirstOrDefault(token => token.AuthenticationResponse == null);
            
            if (nextSecretToken != null)
            {
                await AuthenticateAsync(nextSecretToken);
                
                if (nextSecretToken != null)
                {
                    nextSecretToken.UseCount++;
                }
                return nextSecretToken;
            }
        }
        
        //last resort, delay
        if (nextSecretToken == null)
        {
            nextSecretToken = _secretTokens
                .Where(token => token.TooManyRequestsTimeout.HasValue && DateTime.Now < token.TooManyRequestsTimeout)
                .OrderBy(token => token.TooManyRequestsTimeout)
                .FirstOrDefault(token => token.AuthenticationResponse != null);

            if (nextSecretToken != null && nextSecretToken.TooManyRequestsTimeout.HasValue)
            {
                while (DateTime.Now < nextSecretToken.TooManyRequestsTimeout)
                {
                    TimeSpan delay =  nextSecretToken.TooManyRequestsTimeout.Value - DateTime.Now;
                    TimeSpan delayLeft =  nextSecretToken.TooManyRequestsTimeout.Value - DateTime.Now > TimeSpan.FromMinutes(15) 
                        ? TimeSpan.FromMinutes(15) : nextSecretToken.TooManyRequestsTimeout.Value - DateTime.Now;
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Too many requests to synced artist, waiting {(int)delay.TotalHours}hour(s), {delay.Minutes}minute(s)...");
                    
                    Thread.Sleep(delayLeft);
                }
                nextSecretToken.TooManyRequestsTimeout = null;
            }
        }
        
        if (nextSecretToken != null &&
            string.IsNullOrWhiteSpace(nextSecretToken.AuthenticationResponse?.AccessToken) ||
            (nextSecretToken?.AuthenticationResponse?.ExpiresIn > 0 &&
             DateTime.Now > nextSecretToken.AuthenticationResponse?.ExpiresAt))
        {
            await AuthenticateAsync(nextSecretToken);
        }

        nextSecretToken?.LastUsedTime?.Restart();
        if (nextSecretToken != null)
        {
            nextSecretToken.UseCount++;
        }

        return nextSecretToken;
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
}