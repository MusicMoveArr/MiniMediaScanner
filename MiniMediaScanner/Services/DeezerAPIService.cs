using System.Diagnostics;
using System.Text;
using MiniMediaScanner.Models.Deezer;
using MiniMediaScanner.Models.Tidal;
using Polly;
using Polly.Retry;
using RestSharp;

namespace MiniMediaScanner.Services;

public class DeezerAPIService
{
    private const string SearchArtistsUrl = "https://api.deezer.com/search/artist?q=\"{0}\"&output=json";
    private const string ArtistsIdUrl = "https://api.deezer.com/artist/{0}?output=json";
    private const string AlbumsByArtistIdUrl = "https://api.deezer.com/artist/{0}/albums?output=json";
    private const string AlbumTracksUrl = "https://api.deezer.com/album/{0}/tracks?output=json";
    private const string AlbumByIdUrl = "https://api.deezer.com/album/{0}?output=json";
    private const string TrackByIdUrl = "https://api.deezer.com/track/{0}?output=json";

    public DeezerAPIService()
    {
        
    }
    
    public async Task<DeezerSearchDataModel<DeezerSearchArtistModel>?> SearchResultsArtistsAsync(string searchTerm)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Deezer SearchResultsArtists '{searchTerm}'");
        using RestClient client = new RestClient(string.Format(SearchArtistsUrl, Uri.EscapeDataString(searchTerm)));

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<DeezerSearchDataModel<DeezerSearchArtistModel>>(request);
        });
    }
    
    public async Task<DeezerSearchArtistModel?> GetArtistInfoByIdAsync(long artistId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Deezer GetArtistInfoById '{artistId}'");
        using RestClient client = new RestClient(string.Format(ArtistsIdUrl, artistId));

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<DeezerSearchArtistModel>(request);
        });
    }
    
    public async Task<DeezerSearchDataModel<DeezerAlbumModel>?> GetAlbumsByArtistIdAsync(long artistId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Deezer GetAlbumsByArtistId '{artistId}'");
        using RestClient client = new RestClient(string.Format(AlbumsByArtistIdUrl, artistId));

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<DeezerSearchDataModel<DeezerAlbumModel>>(request);
        });
    }
    public async Task<DeezerSearchDataModel<DeezerAlbumModel>?> GetAlbumsNextAsync(string next)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Deezer GetAlbumsNext '{next}'");
        using RestClient client = new RestClient(next);

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<DeezerSearchDataModel<DeezerAlbumModel>>(request);
        });
    }
    
    public async Task<DeezerSearchDataModel<DeezerAlbumTrackModel>?> GetTracksByAlbumIdAsync(long albumId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Deezer GetTracksByAlbumId '{albumId}'");
        using RestClient client = new RestClient(string.Format(AlbumTracksUrl, albumId));

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<DeezerSearchDataModel<DeezerAlbumTrackModel>>(request);
        });
    }

    public async Task<DeezerTrackModel?> GetTrackByIdAsync(long trackId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Deezer GetTrackById '{trackId}'");
        using RestClient client = new RestClient(string.Format(TrackByIdUrl, trackId));

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<DeezerTrackModel>(request);
        });
    }
    public async Task<DeezerSearchDataModel<DeezerAlbumTrackModel>?> GetAlbumTracksNextAsync(string next)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Deezer GetAlbumTracksNext '{next}'");
        using RestClient client = new RestClient(next);

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<DeezerSearchDataModel<DeezerAlbumTrackModel>>(request);
        });
    }
    public async Task<DeezerAlbumModel?> GetAlbumByIdAsync(long albumId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting Deezer GetAlbumById '{albumId}'");
        using RestClient client = new RestClient(string.Format(AlbumByIdUrl, albumId));

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<DeezerAlbumModel>(request);
        });
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
                });
        
        return retryPolicy;
    }
}