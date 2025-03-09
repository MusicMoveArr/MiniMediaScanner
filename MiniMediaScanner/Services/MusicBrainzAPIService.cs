using System.Diagnostics;
using MiniMediaScanner.Models.MusicBrainz;
using Polly;
using Polly.Retry;
using RestSharp;

namespace MiniMediaScanner.Services;

public class MusicBrainzAPIService
{
    private static Stopwatch _stopwatch = Stopwatch.StartNew();
    
    public MusicBrainzArtistSearchModel? SearchArtist(string artistName)
    {
        RetryPolicy retryPolicy = GetRetryPolicy();
        Console.WriteLine($"Requesting MusicBrainz SearchArtist '{artistName}'");
        string url = $"https://musicbrainz.org/ws/2/artist/?query=artist:'{artistName}' AND (type:person or type:group)&fmt=json";

        return retryPolicy.Execute(() =>
        {
            using RestClient client = new RestClient(url);
            RestRequest request = new RestRequest();
            var response =  client.Get<MusicBrainzArtistSearchModel>(request);
            
            _stopwatch.Restart();
            return response;
        });
    }
    public MusicBrainzArtistModel? GetArtist(Guid musicBrainzArtistId)
    {
        RetryPolicy retryPolicy = GetRetryPolicy();
        Console.WriteLine($"Requesting MusicBrainz GetArtist '{musicBrainzArtistId}'");
        string url = $"https://musicbrainz.org/ws/2/release?artist={musicBrainzArtistId}&fmt=json";

        return retryPolicy.Execute(() =>
        {
            using RestClient client = new RestClient(url);
            RestRequest request = new RestRequest();
            var response =  client.Get<MusicBrainzArtistModel>(request);
            
            _stopwatch.Restart();
            return response;
        });
    }
    public MusicBrainzArtistInfoModel? GetArtistInfo(Guid musicBrainzArtistId)
    {
        RetryPolicy retryPolicy = GetRetryPolicy();
        Console.WriteLine($"Requesting MusicBrainz GetArtistInfo '{musicBrainzArtistId}'");
        string url = $"https://musicbrainz.org/ws/2/artist/{musicBrainzArtistId}?inc=aliases&fmt=json";

        return retryPolicy.Execute(() =>
        {
            using RestClient client = new RestClient(url);
            RestRequest request = new RestRequest();
            var response =   client.Get<MusicBrainzArtistInfoModel>(request);
            
            _stopwatch.Restart();
            return response;
        });
    }
    public MusicBrainzReleaseModel? GetTracks(Guid musicBrainzAlbumId)
    {
        RetryPolicy retryPolicy = GetRetryPolicy();
        Console.WriteLine($"Requesting MusicBrainz Tracks '{musicBrainzAlbumId}'");
        string url = $"https://musicbrainz.org/ws/2/release/{musicBrainzAlbumId}?inc=recordings&fmt=json";

        return retryPolicy.Execute(() =>
        {
            using RestClient client = new RestClient(url);
            RestRequest request = new RestRequest();
            var response =  client.Get<MusicBrainzReleaseModel>(request);
            
            _stopwatch.Restart();
            return response;
        });
            
    }
    public MusicBrainzArtistReleaseModel? GetReleaseWithLabel(Guid musicBrainzReleaseId)
    {
        RetryPolicy retryPolicy = GetRetryPolicy();
        
        Console.WriteLine($"Requesting MusicBrainz GetReleaseWithLabel '{musicBrainzReleaseId}'");
        string url = $"https://musicbrainz.org/ws/2/release/{musicBrainzReleaseId}?inc=labels&fmt=json";

        return retryPolicy.Execute(() =>
        {
            using RestClient client = new RestClient(url);
            RestRequest request = new RestRequest();
            var response =  client.Get<MusicBrainzArtistReleaseModel>(request);
            
            _stopwatch.Restart();
            return response;
        });
    }
    
    public MusicBrainzArtistRelationModel? GetExternalLinks(Guid musicBrainzArtistId)
    {
        RetryPolicy retryPolicy = GetRetryPolicy();
        Console.WriteLine("Requesting MusicBrainz external links");
        string url = $"https://musicbrainz.org/ws/2/artist/{musicBrainzArtistId}?inc=url-rels&fmt=json";

        return retryPolicy.Execute(() =>
        {
            using RestClient client = new RestClient(url);
            
            RestRequest request = new RestRequest();
            var response = client.Get<MusicBrainzArtistRelationModel>(request);

            _stopwatch.Restart();
            return response;
        });
    }
    
    public MusicBrainzArtistModel? GetRecordingById(Guid recordingId)
    {
        RetryPolicy retryPolicy = GetRetryPolicy();
        
        Console.WriteLine($"Requesting MusicBrainz GetRecordingById, {recordingId}");
        string url = $"https://musicbrainz.org/ws/2/recording/{recordingId}?fmt=json&inc=isrcs+artists+releases+release-groups+url-rels+media";

        return retryPolicy.Execute(() =>
        {
            using RestClient client = new RestClient(url);
            RestRequest request = new RestRequest();
            
            var response = client.Get<MusicBrainzArtistModel>(request);

            _stopwatch.Restart();
            return response;
        });
    }
    
    public MusicBrainzArtistModel? GetReleasesForArtist(Guid artistId, int limit, int offset)
    {
        RetryPolicy retryPolicy = GetRetryPolicy();
        var url = $"https://musicbrainz.org/ws/2/release?artist={artistId}&inc=recordings&fmt=json&limit={limit}&offset={offset}";

        Console.WriteLine($"Requesting MusicBrainz Releases '{artistId}', limit '{limit}', offset '{offset}'");

        return retryPolicy.Execute(() =>
        {
            using RestClient client = new RestClient(url);
            RestRequest request = new RestRequest();
            var response = client.Get<MusicBrainzArtistModel>(request);

            _stopwatch.Restart();
            return response;
        });
    }

    private RetryPolicy GetRetryPolicy()
    {
        RetryPolicy retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .WaitAndRetry(5, retryAttempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) => {
                    Console.WriteLine($"Retry {retryCount} after {timeSpan.TotalSeconds} sec due to: {exception.Message}");
                });
        return retryPolicy;
    }
}