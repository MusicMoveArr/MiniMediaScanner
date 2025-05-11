using System.Diagnostics;
using MiniMediaScanner.Models.MusicBrainz;
using Polly;
using Polly.Retry;
using RestSharp;

namespace MiniMediaScanner.Services;

public class MusicBrainzAPIService
{
    public async Task<MusicBrainzArtistSearchModel?> SearchArtistAsync(string artistName)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting MusicBrainz SearchArtist '{artistName}'");
        string url = $"https://musicbrainz.org/ws/2/artist/?query=artist:'{artistName}' AND (type:person or type:group)&fmt=json";
        using RestClient client = new RestClient(url);

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<MusicBrainzArtistSearchModel>(request);
        });
    }
    public async Task<MusicBrainzArtistModel?> GetArtistAsync(Guid musicBrainzArtistId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting MusicBrainz GetArtist '{musicBrainzArtistId}'");
        string url = $"https://musicbrainz.org/ws/2/release?artist={musicBrainzArtistId}&fmt=json";
        using RestClient client = new RestClient(url);

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<MusicBrainzArtistModel>(request);
        });
    }
    public async Task<MusicBrainzArtistInfoModel?> GetArtistInfoAsync(Guid musicBrainzArtistId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting MusicBrainz GetArtistInfo '{musicBrainzArtistId}'");
        string url = $"https://musicbrainz.org/ws/2/artist/{musicBrainzArtistId}?inc=aliases&fmt=json";
        using RestClient client = new RestClient(url);

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<MusicBrainzArtistInfoModel>(request);
        });
    }
    public async Task<MusicBrainzReleaseModel?> GetTracksAsync(Guid musicBrainzAlbumId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine($"Requesting MusicBrainz Tracks '{musicBrainzAlbumId}'");
        string url = $"https://musicbrainz.org/ws/2/release/{musicBrainzAlbumId}?inc=recordings&fmt=json";
        using RestClient client = new RestClient(url);

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<MusicBrainzReleaseModel>(request);
        });
            
    }
    public async Task<MusicBrainzArtistReleaseModel?> GetReleaseWithLabelAsync(Guid musicBrainzReleaseId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        
        Debug.WriteLine($"Requesting MusicBrainz GetReleaseWithLabel '{musicBrainzReleaseId}'");
        string url = $"https://musicbrainz.org/ws/2/release/{musicBrainzReleaseId}?inc=labels&fmt=json";
        using RestClient client = new RestClient(url);

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<MusicBrainzArtistReleaseModel>(request);
        });
    }
    
    public async Task<MusicBrainzArtistRelationModel?> GetExternalLinksAsync(Guid musicBrainzArtistId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        Debug.WriteLine("Requesting MusicBrainz external links");
        string url = $"https://musicbrainz.org/ws/2/artist/{musicBrainzArtistId}?inc=url-rels&fmt=json";
        using RestClient client = new RestClient(url);

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<MusicBrainzArtistRelationModel>(request);
        });
    }
    
    public async Task<MusicBrainzArtistModel?> GetRecordingByIdAsync(Guid recordingId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        
        Debug.WriteLine($"Requesting MusicBrainz GetRecordingById, {recordingId}");
        string url = $"https://musicbrainz.org/ws/2/recording/{recordingId}?fmt=json&inc=isrcs+artists+releases+release-groups+url-rels+media";
        using RestClient client = new RestClient(url);

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<MusicBrainzArtistModel>(request);
        });
    }
    
    public async Task<MusicBrainzArtistModel?> GetReleasesForArtistAsync(Guid artistId, int limit, int offset)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        var url = $"https://musicbrainz.org/ws/2/release?artist={artistId}&fmt=json&limit={limit}&offset={offset}";

        Debug.WriteLine($"Requesting MusicBrainz Releases '{artistId}', limit '{limit}', offset '{offset}'");
        using RestClient client = new RestClient(url);

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            return await client.GetAsync<MusicBrainzArtistModel>(request);
        });
    }
    public async Task<MusicBrainzArtistReleaseModel?> GetReleasesWithRecordingsForArtistAsync(Guid releaseId, int limit, int offset)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        var url = $"https://musicbrainz.org/ws/2/release/{releaseId}?inc=recordings+labels+artists&fmt=json&limit={limit}&offset={offset}";

        Debug.WriteLine($"Requesting MusicBrainz Releases by release id '{releaseId}', limit '{limit}', offset '{offset}'");
        using RestClient client = new RestClient(url);

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            
            return await client.GetAsync<MusicBrainzArtistReleaseModel>(request);
        });
    }
    public async Task<MusicBrainzLabelInfoLabelModel?> GetLabelByIdAsync(Guid labelId)
    {
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        var url = $"https://musicbrainz.org/ws/2/label/{labelId}?fmt=json";

        Debug.WriteLine($"Requesting MusicBrainz Label by id '{labelId}'");
        using RestClient client = new RestClient(url);

        return await retryPolicy.ExecuteAsync(async () =>
        {
            RestRequest request = new RestRequest();
            
            return await client.GetAsync<MusicBrainzLabelInfoLabelModel>(request);
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
                });
        return retryPolicy;
    }
}