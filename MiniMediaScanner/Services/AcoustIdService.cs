using AcoustID;
using AcoustID.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quartz.Logging;
using RestSharp;

namespace MiniMediaScanner.Services;

public class AcoustIdService
{
    public async Task<JObject?> LookupAcoustIdAsync(string acoustIdApiKey, string fingerprint, int duration)
    {
        if (string.IsNullOrWhiteSpace(acoustIdApiKey) || duration <= 0)
        {
            return null;
        }
        
        using var client = new RestClient("https://api.acoustid.org/v2/lookup");
        var request = new RestRequest();
        request.AddParameter("client", acoustIdApiKey);
        request.AddParameter("meta", "recordings");
        request.AddParameter("duration", duration);
        request.AddParameter("fingerprint", fingerprint);

        var response = await client.ExecuteAsync(request);

        if (response.IsSuccessful)
        {
            var content = response.Content;

            if (!string.IsNullOrWhiteSpace(content))
            {
                JObject jsonResponse = JObject.Parse(content);
                
                return jsonResponse;
            }
        }
        else
        {
            Console.WriteLine("Error: " + response.ErrorMessage);
        }
        return null;
    }
}