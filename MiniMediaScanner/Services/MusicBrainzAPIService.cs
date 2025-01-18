using System.Diagnostics;
using MiniMediaScanner.Models;
using MiniMediaScanner.Models.MusicBrainzRecordings;
using RestSharp;

namespace MiniMediaScanner.Services;

public class MusicBrainzAPIService
{
    private Stopwatch _stopwatch = Stopwatch.StartNew();
    
    public MusicBrainzArtistModel? GetArtist(string musicBrainzArtistId)
    {
        DelayAPICall();
        
        Console.WriteLine($"Requesting MusicBrainz GetArtist '{musicBrainzArtistId}'");

        try
        {
            string url = $"https://musicbrainz.org/ws/2/release?artist={musicBrainzArtistId}&fmt=json";

            using RestClient client = new RestClient(url);
            RestRequest request = new RestRequest();
            var response =  client.Get<MusicBrainzArtistModel>(request);
            
            _stopwatch.Restart();
            return response;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }
    public MusicBrainzArtistInfoModel? GetArtistInfo(string musicBrainzArtistId)
    {
        DelayAPICall();

        Console.WriteLine($"Requesting MusicBrainz GetArtistInfo '{musicBrainzArtistId}'");
        
        try
        {
            string url = $"https://musicbrainz.org/ws/2/artist/{musicBrainzArtistId}?inc=aliases&fmt=json";

            using RestClient client = new RestClient(url);
            RestRequest request = new RestRequest();
            var response =   client.Get<MusicBrainzArtistInfoModel>(request);
            _stopwatch.Restart();
            return response;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
        
    }
    public MusicBrainzReleaseModel? GetTracks(string musicBrainzAlbumId)
    {
        DelayAPICall();
        
        Console.WriteLine($"Requesting MusicBrainz Tracks '{musicBrainzAlbumId}'");
        
        try
        {
            string url = $"https://musicbrainz.org/ws/2/release/{musicBrainzAlbumId}?inc=recordings&fmt=json";

            using RestClient client = new RestClient(url);
            RestRequest request = new RestRequest();
            var response =  client.Get<MusicBrainzReleaseModel>(request);
            _stopwatch.Restart();
            return response;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return null;
        }
    }
    public MusicBrainzArtistRelationModel GetExternalLinks(string musicBrainzArtistId)
    {
        DelayAPICall();
        
        Console.WriteLine("Requesting MusicBrainz external links");
        
        try
        {
            string url = $"https://musicbrainz.org/ws/2/artist/{musicBrainzArtistId}?inc=url-rels&fmt=json";

            using RestClient client = new RestClient(url);
            RestRequest request = new RestRequest();
            var response = client.Get<MusicBrainzArtistRelationModel>(request);

            _stopwatch.Restart();
            return response;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return null;
        }
    }
    
    public MusicBrainzArtistModel GetRecordingById(string recordingId)
    {
        DelayAPICall();
        
        Console.WriteLine("Requesting MusicBrainz external links");
        
        try
        {
            string url = $"https://musicbrainz.org/ws/2/recording/{recordingId}?fmt=json&inc=isrcs+artists+releases+release-groups+url-rels+media";

            using RestClient client = new RestClient(url);
            RestRequest request = new RestRequest();
            var response = client.Get<MusicBrainzArtistModel>(request);

            _stopwatch.Restart();
            return response;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return null;
        }
    }
    
    public MusicBrainzArtistModel? GetReleasesForArtist(string artistId, int limit, int offset)
    {
        DelayAPICall();
        
        // Build the URL with the artistId, limit, and offset
        var url = $"https://musicbrainz.org/ws/2/release?artist={artistId}&inc=recordings&fmt=json&limit={limit}&offset={offset}";

        Console.WriteLine($"Requesting MusicBrainz Releases '{artistId}', limit '{limit}', offset '{offset}'");
        
        try
        {
            // Make the HTTP GET request
            using RestClient client = new RestClient(url);
            RestRequest request = new RestRequest();
            var response = client.Get<MusicBrainzArtistModel>(request);

            _stopwatch.Restart();
            return response;
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine(e.Message);
            return null;
        }
    }

    private void DelayAPICall()
    {
        if (_stopwatch.ElapsedMilliseconds < 1000)
        {
            Thread.Sleep((1000 - (int)_stopwatch.ElapsedMilliseconds) + 300);
        }
    }
}