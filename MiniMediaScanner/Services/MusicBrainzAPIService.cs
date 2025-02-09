using System.Diagnostics;
using MiniMediaScanner.Models;
using Polly;
using Polly.Retry;
using RestSharp;

namespace MiniMediaScanner.Services;

public class MusicBrainzAPIService
{
    private static Stopwatch _stopwatch = Stopwatch.StartNew();
    
    public MusicBrainzArtistModel? GetArtist(string musicBrainzArtistId)
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
    public MusicBrainzArtistInfoModel? GetArtistInfo(string musicBrainzArtistId)
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
    public MusicBrainzReleaseModel? GetTracks(string musicBrainzAlbumId)
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
    public MusicBrainzArtistReleaseModel? GetReleaseWithLabel(string musicBrainzReleaseId)
    {
        RetryPolicy retryPolicy = GetRetryPolicy();
        //ServiceUnavailable
        
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
    
    public MusicBrainzArtistRelationModel? GetExternalLinks(string musicBrainzArtistId)
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
    
    public MusicBrainzArtistModel? GetRecordingById(string recordingId)
    {
        RetryPolicy retryPolicy = GetRetryPolicy();
        //ServiceUnavailable
        
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
    
    public MusicBrainzArtistModel? GetReleasesForArtist(string artistId, int limit, int offset)
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
            .WaitAndRetry(5, retryAttempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) => {
                    Console.WriteLine($"Retry {retryCount} after {timeSpan.TotalSeconds} sec due to: {exception.Message}");
                });
        return retryPolicy;
    }
}