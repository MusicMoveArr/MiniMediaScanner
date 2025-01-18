using AcoustID;
using AcoustID.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace MiniMediaScanner.Services;

public class AcoustIdService
{
    public string? GetMusicBrainzRecordingId(string acoustIdApiKey, string fingerprint, int duration)
    {
        if (string.IsNullOrWhiteSpace(acoustIdApiKey))
        {
            return null;
        }
        
        var client = new RestClient("https://api.acoustid.org/v2/lookup");
        var request = new RestRequest();
        request.AddParameter("client", acoustIdApiKey);
        request.AddParameter("meta", "recordings");
        request.AddParameter("duration", duration);
        request.AddParameter("fingerprint", fingerprint);

        var response = client.Execute(request);

        if (response.IsSuccessful)
        {
            var content = response.Content;

            if (!string.IsNullOrWhiteSpace(content))
            {
                JObject jsonResponse = JObject.Parse(content);
                var recordingId = jsonResponse["results"]?[0]?["recordings"]?[0]?["id"]?.ToString();
                return recordingId;
            }
        }
        else
        {
            Console.WriteLine("Error: " + response.ErrorMessage);
        }
        return null;
    }
}