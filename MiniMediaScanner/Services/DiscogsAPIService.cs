using System.Diagnostics;
using MiniMediaScanner.Models.Discogs;
using Polly;
using Polly.Retry;
using RestSharp;

namespace MiniMediaScanner.Services;

public class DiscogsAPIService
{
    private const string AccessTokenUrl = "https://api.discogs.com/oauth/access_token";
    private const string AuthorizeUrl = "https://www.discogs.com/oauth/authorize";
    private const string RequestTokenUrl = "https://api.discogs.com/oauth/request_token";
    private const string ArtistIdUrl = "https://api.discogs.com/artists/{0}?token={1}";
    private const string ReleaseIdUrl = "https://api.discogs.com/releases/{0}?token={1}";
    private string _discogsToken;

    public DiscogsAPIService(string discogsToken)
    {
        _discogsToken = discogsToken;
    }
    
    
    public async Task<DiscogsArtistModel?> GetArtistByIdAsync(int artistId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Discogs GetArtistByIdAsync '{artistId}'");

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestClientOptions options = new RestClientOptions(string.Format(ArtistIdUrl,  artistId, _discogsToken));
            
            using RestClient client = new RestClient(options);
            RestRequest request = new RestRequest();
            return await client.GetAsync<DiscogsArtistModel>(request);
        });
    }
    public async Task<DiscogsReleaseModel?> GetReleaseByIdAsync(int releaseId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Discogs GetReleaseByIdAsync '{releaseId}'");

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestClientOptions options = new RestClientOptions(string.Format(ReleaseIdUrl,  releaseId, _discogsToken));
            
            using RestClient client = new RestClient(options);
            RestRequest request = new RestRequest();
            return await client.GetAsync<DiscogsReleaseModel>(request);
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
                });
        
        return retryPolicy;
    }
}