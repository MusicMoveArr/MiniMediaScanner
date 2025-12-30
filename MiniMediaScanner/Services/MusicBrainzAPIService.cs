using System.Diagnostics;
using MiniMediaScanner.Models.MusicBrainz;
using Polly;
using Polly.Retry;
using RestSharp;

namespace MiniMediaScanner.Services;

public class MusicBrainzAPIService
{
    private const int _waitMilliseconds = 1000;
    private Stopwatch _waitStopwatch = Stopwatch.StartNew();
    private SemaphoreSlim _semaphore = new SemaphoreSlim(1);

    private void WaitForNextCall()
    {
        int waitTime = _waitMilliseconds - (int)_waitStopwatch.ElapsedMilliseconds;
        if (waitTime > 0)
        {
            Thread.Sleep(waitTime);
        }
    }
    
    public async Task<MusicBrainzArtistSearchModel?> SearchArtistAsync(string artistName)
    {
        await _semaphore.WaitAsync();
        WaitForNextCall();
        
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting MusicBrainz SearchArtist '{artistName}'");
        string url = $"https://musicbrainz.org/ws/2/artist/?query=artist:'{artistName}' AND (type:person or type:group)&fmt=json";
        using RestClient client = new RestClient(url);

        var result = await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<MusicBrainzArtistSearchModel>(request);
        });
        
        _waitStopwatch.Restart();
        _semaphore.Release();
        return result;
    }
    public async Task<MusicBrainzArtistModel?> GetArtistAsync(Guid musicBrainzArtistId)
    {
        await _semaphore.WaitAsync();
        WaitForNextCall();

        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting MusicBrainz GetArtist '{musicBrainzArtistId}'");
        string url = $"https://musicbrainz.org/ws/2/release?artist={musicBrainzArtistId}&fmt=json";
        using RestClient client = new RestClient(url);

        var result = await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<MusicBrainzArtistModel>(request);
        });
        
        _waitStopwatch.Restart();
        _semaphore.Release();
        return result;
    }
    public async Task<MusicBrainzArtistInfoModel?> GetArtistInfoAsync(Guid musicBrainzArtistId)
    {
        await _semaphore.WaitAsync();
        WaitForNextCall();

        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting MusicBrainz GetArtistInfo '{musicBrainzArtistId}'");
        string url = $"https://musicbrainz.org/ws/2/artist/{musicBrainzArtistId}?inc=aliases&fmt=json";
        using RestClient client = new RestClient(url);

        var result = await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<MusicBrainzArtistInfoModel>(request);
        });
        
        _waitStopwatch.Restart();
        _semaphore.Release();
        return result;
    }
    public async Task<MusicBrainzReleaseModel?> GetTracksAsync(Guid musicBrainzAlbumId)
    {
        await _semaphore.WaitAsync();
        WaitForNextCall();

        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting MusicBrainz Tracks '{musicBrainzAlbumId}'");
        string url = $"https://musicbrainz.org/ws/2/release/{musicBrainzAlbumId}?inc=recordings&fmt=json";
        using RestClient client = new RestClient(url);

        var result = await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<MusicBrainzReleaseModel>(request);
        });
        
        _waitStopwatch.Restart();
        _semaphore.Release();
        return result;
    }
    public async Task<MusicBrainzArtistReleaseModel?> GetReleaseWithLabelAsync(Guid musicBrainzReleaseId)
    {
        await _semaphore.WaitAsync();
        WaitForNextCall();

        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        
        Debug.WriteLine($"Requesting MusicBrainz GetReleaseWithLabel '{musicBrainzReleaseId}'");
        string url = $"https://musicbrainz.org/ws/2/release/{musicBrainzReleaseId}?inc=labels&fmt=json";
        using RestClient client = new RestClient(url);

        var result = await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<MusicBrainzArtistReleaseModel>(request);
        });
        
        _waitStopwatch.Restart();
        _semaphore.Release();
        return result;
    }
    
    public async Task<MusicBrainzArtistRelationModel?> GetExternalLinksAsync(Guid musicBrainzArtistId)
    {
        await _semaphore.WaitAsync();
        WaitForNextCall();

        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine("Requesting MusicBrainz external links");
        string url = $"https://musicbrainz.org/ws/2/artist/{musicBrainzArtistId}?inc=url-rels&fmt=json";
        using RestClient client = new RestClient(url);

        var result = await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<MusicBrainzArtistRelationModel>(request);
        });
        
        _waitStopwatch.Restart();
        _semaphore.Release();
        return result;
    }
    
    public async Task<MusicBrainzArtistModel?> GetRecordingByIdAsync(Guid recordingId)
    {
        await _semaphore.WaitAsync();
        WaitForNextCall();

        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        
        Debug.WriteLine($"Requesting MusicBrainz GetRecordingById, {recordingId}");
        string url = $"https://musicbrainz.org/ws/2/recording/{recordingId}?fmt=json&inc=isrcs+artists+releases+release-groups+url-rels+media";
        using RestClient client = new RestClient(url);

        var result = await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<MusicBrainzArtistModel>(request);
        });
        
        _waitStopwatch.Restart();
        _semaphore.Release();
        return result;
    }
    
    public async Task<MusicBrainzArtistModel?> GetReleasesForArtistAsync(Guid artistId, int limit, int offset)
    {
        await _semaphore.WaitAsync();
        WaitForNextCall();

        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        var url = $"https://musicbrainz.org/ws/2/release?artist={artistId}&fmt=json&limit={limit}&offset={offset}";

        Debug.WriteLine($"Requesting MusicBrainz Releases '{artistId}', limit '{limit}', offset '{offset}'");
        using RestClient client = new RestClient(url);

        var result = await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<MusicBrainzArtistModel>(request);
        });
        
        _waitStopwatch.Restart();
        _semaphore.Release();
        return result;
    }
    public async Task<MusicBrainzArtistReleaseModel?> GetReleasesWithRecordingsForArtistAsync(Guid releaseId, int limit, int offset)
    {
        await _semaphore.WaitAsync();
        WaitForNextCall();

        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        var url = $"https://musicbrainz.org/ws/2/release/{releaseId}?inc=recordings+labels+artists&fmt=json&limit={limit}&offset={offset}";

        Debug.WriteLine($"Requesting MusicBrainz Releases by release id '{releaseId}', limit '{limit}', offset '{offset}'");
        using RestClient client = new RestClient(url);

        var result = await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<MusicBrainzArtistReleaseModel>(request);
        });
        
        _waitStopwatch.Restart();
        _semaphore.Release();
        return result;
    }
    public async Task<MusicBrainzLabelInfoLabelModel?> GetLabelByIdAsync(Guid labelId)
    {
        await _semaphore.WaitAsync();
        WaitForNextCall();

        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        var url = $"https://musicbrainz.org/ws/2/label/{labelId}?fmt=json";

        Debug.WriteLine($"Requesting MusicBrainz Label by id '{labelId}'");
        using RestClient client = new RestClient(url);

        var result = await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<MusicBrainzLabelInfoLabelModel>(request);
        });
        
        _waitStopwatch.Restart();
        _semaphore.Release();
        return result;
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
                });
        return retryPolicy;
    }
}