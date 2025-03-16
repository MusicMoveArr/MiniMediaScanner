using System.Diagnostics;
using MiniMediaScanner.Models.MusicBrainz;
using Polly;
using Polly.Retry;
using RestSharp;

namespace MiniMediaScanner.Services;

public class MusicBrainzAPIService
{
    private static Stopwatch _stopwatch = Stopwatch.StartNew();
    
    public async Task<MusicBrainzArtistSearchModel?> SearchArtistAsync(string artistName)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Console.WriteLine($"Requesting MusicBrainz SearchArtist '{artistName}'");
        string url = $"https://musicbrainz.org/ws/2/artist/?query=artist:'{artistName}' AND (type:person or type:group)&fmt=json";

        return await retryPolicy.ExecuteAsync(async () =>
        {
            using RestClient client = new RestClient(url);
            RestRequest request = new RestRequest();
            var response = await client.GetAsync<MusicBrainzArtistSearchModel>(request);
            
            _stopwatch.Restart();
            return response;
        });
    }
    public async Task<MusicBrainzArtistModel?> GetArtistAsync(Guid musicBrainzArtistId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Console.WriteLine($"Requesting MusicBrainz GetArtist '{musicBrainzArtistId}'");
        string url = $"https://musicbrainz.org/ws/2/release?artist={musicBrainzArtistId}&fmt=json";

        return await retryPolicy.ExecuteAsync(async () =>
        {
            using RestClient client = new RestClient(url);
            RestRequest request = new RestRequest();
            var response = await client.GetAsync<MusicBrainzArtistModel>(request);
            
            _stopwatch.Restart();
            return response;
        });
    }
    public async Task<MusicBrainzArtistInfoModel?> GetArtistInfoAsync(Guid musicBrainzArtistId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Console.WriteLine($"Requesting MusicBrainz GetArtistInfo '{musicBrainzArtistId}'");
        string url = $"https://musicbrainz.org/ws/2/artist/{musicBrainzArtistId}?inc=aliases&fmt=json";

        return await retryPolicy.ExecuteAsync(async () =>
        {
            using RestClient client = new RestClient(url);
            RestRequest request = new RestRequest();
            var response = await client.GetAsync<MusicBrainzArtistInfoModel>(request);
            
            _stopwatch.Restart();
            return response;
        });
    }
    public async Task<MusicBrainzReleaseModel?> GetTracksAsync(Guid musicBrainzAlbumId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Console.WriteLine($"Requesting MusicBrainz Tracks '{musicBrainzAlbumId}'");
        string url = $"https://musicbrainz.org/ws/2/release/{musicBrainzAlbumId}?inc=recordings&fmt=json";

        return await retryPolicy.ExecuteAsync(async () =>
        {
            using RestClient client = new RestClient(url);
            RestRequest request = new RestRequest();
            var response = await client.GetAsync<MusicBrainzReleaseModel>(request);
            
            _stopwatch.Restart();
            return response;
        });
            
    }
    public async Task<MusicBrainzArtistReleaseModel?> GetReleaseWithLabelAsync(Guid musicBrainzReleaseId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        
        Console.WriteLine($"Requesting MusicBrainz GetReleaseWithLabel '{musicBrainzReleaseId}'");
        string url = $"https://musicbrainz.org/ws/2/release/{musicBrainzReleaseId}?inc=labels&fmt=json";

        return await retryPolicy.ExecuteAsync(async () =>
        {
            using RestClient client = new RestClient(url);
            RestRequest request = new RestRequest();
            var response = await client.GetAsync<MusicBrainzArtistReleaseModel>(request);
            
            _stopwatch.Restart();
            return response;
        });
    }
    
    public async Task<MusicBrainzArtistRelationModel?> GetExternalLinksAsync(Guid musicBrainzArtistId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Console.WriteLine("Requesting MusicBrainz external links");
        string url = $"https://musicbrainz.org/ws/2/artist/{musicBrainzArtistId}?inc=url-rels&fmt=json";

        return await retryPolicy.ExecuteAsync(async () =>
        {
            using RestClient client = new RestClient(url);
            
            RestRequest request = new RestRequest();
            var response = await client.GetAsync<MusicBrainzArtistRelationModel>(request);

            _stopwatch.Restart();
            return response;
        });
    }
    
    public async Task<MusicBrainzArtistModel?> GetRecordingByIdAsync(Guid recordingId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        
        Console.WriteLine($"Requesting MusicBrainz GetRecordingById, {recordingId}");
        string url = $"https://musicbrainz.org/ws/2/recording/{recordingId}?fmt=json&inc=isrcs+artists+releases+release-groups+url-rels+media";

        return await retryPolicy.ExecuteAsync(async () =>
        {
            using RestClient client = new RestClient(url);
            RestRequest request = new RestRequest();
            
            var response = await client.GetAsync<MusicBrainzArtistModel>(request);

            _stopwatch.Restart();
            return response;
        });
    }
    
    public async Task<MusicBrainzArtistModel?> GetReleasesForArtistAsync(Guid artistId, int limit, int offset)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        var url = $"https://musicbrainz.org/ws/2/release?artist={artistId}&inc=recordings&fmt=json&limit={limit}&offset={offset}";

        Console.WriteLine($"Requesting MusicBrainz Releases '{artistId}', limit '{limit}', offset '{offset}'");

        return await retryPolicy.ExecuteAsync(async () =>
        {
            using RestClient client = new RestClient(url);
            RestRequest request = new RestRequest();
            var response = await client.GetAsync<MusicBrainzArtistModel>(request);

            _stopwatch.Restart();
            return response;
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
                    Console.WriteLine($"Retry {retryCount} after {timeSpan.TotalSeconds} sec due to: {exception.Message}");
                });
        return retryPolicy;
    }
}