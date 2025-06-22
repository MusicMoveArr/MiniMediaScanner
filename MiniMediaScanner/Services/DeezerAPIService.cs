using System.Diagnostics;
using System.Text;
using System.Text.Json;
using MiniMediaScanner.Enums;
using MiniMediaScanner.Models;
using MiniMediaScanner.Models.Deezer;
using MiniMediaScanner.Models.Tidal;
using Polly;
using Polly.Retry;
using RestSharp;
using Swan;

namespace MiniMediaScanner.Services;

public class DeezerAPIService
{
    
    private const string SearchArtistsUrl = "https://api.deezer.com/search/artist?q=\"{0}\"&output=json";
    private const string ArtistsIdUrl = "https://api.deezer.com/artist/{0}?output=json";
    private const string AlbumsByArtistIdUrl = "https://api.deezer.com/artist/{0}/albums?output=json";
    private const string AlbumTracksUrl = "https://api.deezer.com/album/{0}/tracks?output=json";
    private const string AlbumByIdUrl = "https://api.deezer.com/album/{0}?output=json";
    private const string TrackByIdUrl = "https://api.deezer.com/track/{0}?output=json";

    public ProxyManagerService ProxyManagerService { get; private set; }
    
    public DeezerAPIService(string proxyFile, string singleProxy, string proxyMode)
    {
        ProxyManagerService = new ProxyManagerService("https://deezer.com", proxyFile, singleProxy, proxyMode);
    }
    
    public async Task<DeezerSearchDataModel<DeezerSearchArtistModel>?> SearchResultsArtistsAsync(string searchTerm)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Deezer SearchResultsArtists '{searchTerm}'");
        
        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestClientOptions options = new RestClientOptions(string.Format(SearchArtistsUrl, Uri.EscapeDataString(searchTerm)));

            await ProxyManagerService.SetProxySettingsAsync(options);
            using RestClient client = new RestClient(options);
            RestRequest request = new RestRequest();
            
            var response = await client.GetAsync<DeezerSearchDataModel<DeezerSearchArtistModel>>(request);
            response?.Error?.ThrowExceptionOnRateLimiter();
            return response;
        });
    }
    
    public async Task<DeezerSearchArtistModel?> GetArtistInfoByIdAsync(long artistId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Deezer GetArtistInfoById '{artistId}'");
        
        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestClientOptions options = new RestClientOptions(string.Format(ArtistsIdUrl, artistId));
            await ProxyManagerService.SetProxySettingsAsync(options);
            using RestClient client = new RestClient(options);
            
            RestRequest request = new RestRequest();
            var response = await client.GetAsync<DeezerSearchArtistModel>(request);
            response?.Error?.ThrowExceptionOnRateLimiter();
            return response;
        });
    }
    
    public async Task<DeezerSearchDataModel<DeezerAlbumModel>?> GetAlbumsByArtistIdAsync(long artistId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Deezer GetAlbumsByArtistId '{artistId}'");
        

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestClientOptions options = new RestClientOptions(string.Format(AlbumsByArtistIdUrl, artistId));
            await ProxyManagerService.SetProxySettingsAsync(options);
        
            using RestClient client = new RestClient(options);

            RestRequest request = new RestRequest();
            var response = await client.GetAsync<DeezerSearchDataModel<DeezerAlbumModel>>(request);
            response?.Error?.ThrowExceptionOnRateLimiter();
            return response;
        });
    }
    public async Task<DeezerSearchDataModel<DeezerAlbumModel>?> GetAlbumsNextAsync(string next)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Deezer GetAlbumsNext '{next}'");
        
        RestClientOptions options = new RestClientOptions(next);
        await ProxyManagerService.SetProxySettingsAsync(options);
        
        using RestClient client = new RestClient(options);

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            var response = await client.GetAsync<DeezerSearchDataModel<DeezerAlbumModel>>(request);
            response?.Error?.ThrowExceptionOnRateLimiter();
            return response;
        });
    }
    
    public async Task<DeezerSearchDataModel<DeezerAlbumTrackModel>?> GetTracksByAlbumIdAsync(long albumId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Deezer GetTracksByAlbumId '{albumId}'");

        return await retryPolicy.ExecuteAsync(async () =>
        {        
            RestClientOptions options = new RestClientOptions(string.Format(AlbumTracksUrl, albumId));
            await ProxyManagerService.SetProxySettingsAsync(options);
        
            using RestClient client = new RestClient(options);

            RestRequest request = new RestRequest();
            var response = await client.GetAsync<DeezerSearchDataModel<DeezerAlbumTrackModel>>(request);
            response?.Error?.ThrowExceptionOnRateLimiter();
            return response;
        });
    }

    public async Task<DeezerTrackModel?> GetTrackByIdAsync(long trackId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Deezer GetTrackById '{trackId}'");

        return await retryPolicy.ExecuteAsync(async () =>
        {        
            RestClientOptions options = new RestClientOptions(string.Format(TrackByIdUrl, trackId));
            await ProxyManagerService.SetProxySettingsAsync(options);
        
            using RestClient client = new RestClient(options);

            RestRequest request = new RestRequest();
            var response = await client.GetAsync<DeezerTrackModel>(request);
            response?.Error?.ThrowExceptionOnRateLimiter();
            return response;
        });
    }
    public async Task<DeezerSearchDataModel<DeezerAlbumTrackModel>?> GetAlbumTracksNextAsync(string next)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Deezer GetAlbumTracksNext '{next}'");

        return await retryPolicy.ExecuteAsync(async () =>
        {        
            RestClientOptions options = new RestClientOptions(next);
            await ProxyManagerService.SetProxySettingsAsync(options);
        
            using RestClient client = new RestClient(options);

            RestRequest request = new RestRequest();
            var response = await client.GetAsync<DeezerSearchDataModel<DeezerAlbumTrackModel>>(request);
            response?.Error?.ThrowExceptionOnRateLimiter();
            return response;
        });
    }
    public async Task<DeezerAlbumModel?> GetAlbumByIdAsync(long albumId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Deezer GetAlbumById '{albumId}'");
 
        return await retryPolicy.ExecuteAsync(async () =>
        {       
            RestClientOptions options = new RestClientOptions(string.Format(AlbumByIdUrl, albumId));
            await ProxyManagerService.SetProxySettingsAsync(options);
        
            using RestClient client = new RestClient(options);

            RestRequest request = new RestRequest();
            var response = await client.GetAsync<DeezerAlbumModel>(request);
            response?.Error?.ThrowExceptionOnRateLimiter();
            return response;
        });
    }
    
    private AsyncRetryPolicy GetRetryPolicy()
    {
        AsyncRetryPolicy retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .Or<InvalidOperationException>()
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