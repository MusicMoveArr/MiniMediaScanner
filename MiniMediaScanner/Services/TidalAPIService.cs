using System.Diagnostics;
using System.Text;
using MiniMediaScanner.Enums;
using MiniMediaScanner.Models.Tidal;
using Polly;
using Polly.Retry;
using RestSharp;

namespace MiniMediaScanner.Services;

public class TidalAPIService
{
    private const string AuthTokenUrl = "https://auth.tidal.com/v1/oauth2/token";
    private const string SearchResultArtistsUrl = "https://openapi.tidal.com/v2/searchResults/";
    private const string ArtistsIdUrl = "https://openapi.tidal.com/v2/artists/{0}";
    private const string TracksByAlbumIdUrl = "https://openapi.tidal.com/v2/albums/{0}";
    private const string TracksUrl = "https://openapi.tidal.com/v2/tracks";
    private const string TidalApiPrefix = "https://openapi.tidal.com/v2";

    public const int ApiDelay = 4500;
    private List<TidalTokenClientSecret> _secretTokens;
    private readonly string _countryCode;
    public ProxyManagerService ProxyManagerService { get; private set; }

    public TidalAPIService(
        List<TidalTokenClientSecret> secretTokens,
        string countryCode, 
        string proxyFile, 
        string singleProxy, 
        string proxyMode)
    {
        _secretTokens = secretTokens;
        _countryCode = countryCode;
        ProxyManagerService = new ProxyManagerService("https://tidal.com", proxyFile, singleProxy, proxyMode);
    }
    
    public async Task<TidalAuthenticationResponse?> AuthenticateAsync(TidalTokenClientSecret secretToken)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Tidal Authenticate ClientId '{secretToken.ClientId}'");

        var token = await retryPolicy.ExecuteAsync(async () =>
        {
            RestClientOptions options = new RestClientOptions(AuthTokenUrl);
            await ProxyManagerService.SetProxySettingsAsync(options);
            using RestClient client = new RestClient(options);
            RestRequest request = new RestRequest();
            
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretToken.ClientId}:{secretToken.ClientSecret}"));
            request.AddHeader("Authorization", $"Basic {credentials}");
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("grant_type", "client_credentials");
            
            return await client.PostAsync<TidalAuthenticationResponse>(request);
        });

        if (token != null)
        {
            secretToken.AuthenticationResponse = token;
            secretToken.AuthenticationResponse.RequestedAt = DateTime.Now;
        }
        return token;
    }
    
    public async Task<TidalSearchResponse?> SearchResultsArtistsAsync(string searchTerm)
    {
        TidalTokenClientSecret? secretToken = await GetNextTokenSecretAsync();
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Tidal SearchResults '{searchTerm}'");

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestClientOptions options = new RestClientOptions(SearchResultArtistsUrl + Uri.EscapeDataString(searchTerm));
            await ProxyManagerService.SetProxySettingsAsync(options);
            using RestClient client = new RestClient(options);
            RestRequest request = new RestRequest();
            request.AddHeader("Authorization", $"Bearer {secretToken.AuthenticationResponse.AccessToken}");
            request.AddHeader("Accept", "application/vnd.api+json");
            request.AddHeader("Content-Type", "application/vnd.api+json");
            request.AddParameter("countryCode", _countryCode);
            request.AddParameter("include", "artists");
            
            return await client.GetAsync<TidalSearchResponse>(request);
        });
    }
    
    public async Task<TidalSearchResponse?> GetArtistInfoByIdAsync(int artistId)
    {
        TidalTokenClientSecret? secretToken = await GetNextTokenSecretAsync();
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Tidal GetArtistById '{artistId}'");

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestClientOptions options = new RestClientOptions(string.Format(ArtistsIdUrl, artistId));
            await ProxyManagerService.SetProxySettingsAsync(options);
            using RestClient client = new RestClient(options);
            RestRequest request = new RestRequest();
            request.AddHeader("Authorization", $"Bearer {secretToken.AuthenticationResponse.AccessToken}");
            request.AddHeader("Accept", "application/vnd.api+json");
            request.AddHeader("Content-Type", "application/vnd.api+json");
            request.AddParameter("countryCode", _countryCode);
            request.AddParameter("include", "albums,profileArt");

            return await client.GetAsync<TidalSearchResponse>(request);
        });
    }
    
    public async Task<TidalSearchArtistNextResponse?> GetArtistNextInfoByIdAsync(int artistId, string next)
    {
        TidalTokenClientSecret? secretToken = await GetNextTokenSecretAsync();
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Tidal GetArtistNextInfoById '{artistId}'");

        string url = $"{TidalApiPrefix}{next}";

        if (!url.Contains("include="))
        {
            url += "&include=albums,profileArt";
        }

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestClientOptions options = new RestClientOptions(url);
            await ProxyManagerService.SetProxySettingsAsync(options);
            using RestClient client = new RestClient(options);
            RestRequest request = new RestRequest();
            request.AddHeader("Authorization", $"Bearer {secretToken.AuthenticationResponse.AccessToken}");
            request.AddHeader("Accept", "application/vnd.api+json");
            request.AddHeader("Content-Type", "application/vnd.api+json");
            
            return await client.GetAsync<TidalSearchArtistNextResponse>(request);
        });
    }
    
    public async Task<TidalSearchResponse?> GetTracksByAlbumIdAsync(int albumId)
    {
        TidalTokenClientSecret? secretToken = await GetNextTokenSecretAsync();
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Tidal GetTracksByAlbumId '{albumId}'");

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestClientOptions options = new RestClientOptions(string.Format(TracksByAlbumIdUrl, albumId));
            await ProxyManagerService.SetProxySettingsAsync(options);
            using RestClient client = new RestClient(options);
            RestRequest request = new RestRequest();
            request.AddHeader("Authorization", $"Bearer {secretToken.AuthenticationResponse.AccessToken}");
            request.AddHeader("Accept", "application/vnd.api+json");
            request.AddHeader("Content-Type", "application/vnd.api+json");
            request.AddParameter("countryCode", _countryCode);
            request.AddParameter("include", "artists,coverArt,items,providers");
            
            return await client.GetAsync<TidalSearchResponse>(request);
        });
    }
    
    public async Task<TidalSearchTracksNextResponse?> GetTracksNextByAlbumIdAsync(int albumId, string next)
    {
        TidalTokenClientSecret? secretToken = await GetNextTokenSecretAsync();
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Tidal GetTracksNextByAlbumId '{albumId}'");
        
        string url = $"{TidalApiPrefix}{next}";

        if (!url.Contains("include="))
        {
            url += "&include=artists,coverArt,items,providers";
        }

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestClientOptions options = new RestClientOptions(url);
            await ProxyManagerService.SetProxySettingsAsync(options);
            using RestClient client = new RestClient(options);
            RestRequest request = new RestRequest();
            request.AddHeader("Authorization", $"Bearer {secretToken.AuthenticationResponse.AccessToken}");
            request.AddHeader("Accept", "application/vnd.api+json");
            request.AddHeader("Content-Type", "application/vnd.api+json");

            return await client.GetAsync<TidalSearchTracksNextResponse>(request);
        });
    }
    
    public async Task<TidalTrackArtistResponse?> GetTrackArtistsByTrackIdAsync(int[] trackIds)
    {
        TidalTokenClientSecret? secretToken = await GetNextTokenSecretAsync();
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Tidal GetTrackArtistsByTrackId for {trackIds.Length} tracks");

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestClientOptions options = new RestClientOptions(TracksUrl);
            await ProxyManagerService.SetProxySettingsAsync(options);
            using RestClient client = new RestClient(options);
            RestRequest request = new RestRequest();
            request.AddHeader("Authorization", $"Bearer {secretToken.AuthenticationResponse.AccessToken}");
            request.AddHeader("Accept", "application/vnd.api+json");
            request.AddHeader("Content-Type", "application/vnd.api+json");
            request.AddParameter("filter[id]", string.Join(',', trackIds));
            request.AddParameter("include", "artists");
            request.AddParameter("countryCode", _countryCode);
            
            return await client.GetAsync<TidalTrackArtistResponse>(request);
        });
    }

    private async Task<TidalTokenClientSecret?> GetNextTokenSecretAsync()
    {
        //get secret token that was used >ApiDelay time
        TidalTokenClientSecret? nextSecretToken = _secretTokens
            .Where(token => token.AuthenticationResponse != null)
            .FirstOrDefault(token => token.LastUsedTime.ElapsedMilliseconds > ApiDelay);

        //authenticate another secret token
        if (nextSecretToken == null)
        {
            nextSecretToken = _secretTokens.FirstOrDefault(token => token.AuthenticationResponse == null);
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
            nextSecretToken = _secretTokens.FirstOrDefault(token => token.AuthenticationResponse != null);
            Thread.Sleep(ApiDelay);
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
    
    private AsyncRetryPolicy GetRetryPolicy()
    {
        AsyncRetryPolicy retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(5, retryAttempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) => {
                    Debug.WriteLine($"Retry {retryCount} after {timeSpan.TotalSeconds} sec due to: {exception.Message}");
                    Console.WriteLine($"Retry {retryCount} after {timeSpan.TotalSeconds} sec due to: {exception.Message}");
                    
                    if (ProxyManagerService.ProxyMode == ProxyModeType.StickyTillError)
                    {
                        ProxyManagerService.PickNextProxy();
                    }
                });
        
        return retryPolicy;
    }
}