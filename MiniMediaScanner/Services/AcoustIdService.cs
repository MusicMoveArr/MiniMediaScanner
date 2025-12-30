using System.Diagnostics;
using AcoustID;
using AcoustID.Web;
using MiniMediaScanner.Models;
using MiniMediaScanner.Models.AcoustId;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Retry;
using Quartz.Logging;
using RestSharp;

namespace MiniMediaScanner.Services;

public class AcoustIdService
{
    public async Task<AcoustIdResponse?> LookupAcoustIdAsync(string acoustIdApiKey, string fingerprint, int duration)
    {
        if (string.IsNullOrWhiteSpace(acoustIdApiKey))
        {
            return null;
        }
        
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        var client = new RestClient("https://api.acoustid.org/v2/lookup");

        return await retryPolicy.ExecuteAsync(async () =>
        {
            var request = new RestRequest();
            request.AddParameter("client", acoustIdApiKey);
            request.AddParameter("meta", "recordings");
            request.AddParameter("duration", duration);
            request.AddParameter("fingerprint", fingerprint);

            return await client.GetAsync<AcoustIdResponse>(request);
        });
    }
    
    public async Task<AcoustIdSubmitResponse?> SubmitAsync(string acoustIdClientKey, string acoustIdUserKey, List<MetadataModel> tracks)
    {
        if (string.IsNullOrWhiteSpace(acoustIdClientKey) ||
            string.IsNullOrWhiteSpace(acoustIdUserKey) ||
            tracks.Count == 0)
        {
            return null;
        }
        
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        var client = new RestClient("https://api.acoustid.org/v2/submit");

        return await retryPolicy.ExecuteAsync(async () =>
        {
            var request = new RestRequest();
            request.AddParameter("client", acoustIdClientKey);
            request.AddParameter("user", acoustIdUserKey);

            int index = 0;
            foreach (var track in tracks)
            {
                var mediaTags = JsonConvert.DeserializeObject<Dictionary<string, string>>(track.Tag_AllJsonTags);
                string albumArtist = string.Empty;
                if (mediaTags.TryGetValue("Bitrate", out string bitrate))
                {
                    request.AddParameter($"bitrate.{index}", bitrate);
                }
                if (mediaTags.TryGetValue("Year", out string year))
                {
                    request.AddParameter($"year.{index}", year);
                }
                
                if (mediaTags.TryGetValue("album_artist", out albumArtist) ||
                    mediaTags.TryGetValue("AlbumArtist", out albumArtist))
                {
                    request.AddParameter($"albumartist.{index}", albumArtist);
                }

                AddNonEmptyParameter(request, $"duration.{index}", (int)track.TrackLength.TotalSeconds);
                AddNonEmptyParameter(request, $"fingerprint.{index}", track.Tag_AcoustIdFingerprint);
                AddNonEmptyParameter(request, $"fileformat.{index}", track.Path.Substring(track.Path.LastIndexOf('.')+1));
                AddNonEmptyParameter(request, $"track.{index}", track.Title);
                AddNonEmptyParameter(request, $"artist.{index}", track.ArtistName);
                AddNonEmptyParameter(request, $"album.{index}", track.AlbumName);
                AddNonEmptyParameter(request, $"trackno.{index}", track.Tag_Track);
                AddNonEmptyParameter(request, $"discno.{index}", track.Tag_Disc);
                
                index++;
            }
            return await client.PostAsync<AcoustIdSubmitResponse>(request);
        });
    }
    
    public async Task<AcoustIdCheckResponse?> CheckStatusAsync(string acoustIdClientKey, List<int> ids)
    {
        if (string.IsNullOrWhiteSpace(acoustIdClientKey) ||
            ids.Count == 0)
        {
            return null;
        }
        
        AsyncRetryPolicy retryPolicy = GetRetryPolicy();
        var client = new RestClient("https://api.acoustid.org/v2/submission_status");

        return await retryPolicy.ExecuteAsync(async () =>
        {
            var request = new RestRequest();
            request.AddParameter("client", acoustIdClientKey);

            foreach (var id in ids)
            {
                request.AddParameter("id", id);
            }
            return await client.PostAsync<AcoustIdCheckResponse>(request);
        });
    }

    private void AddNonEmptyParameter(RestRequest request, string parameterName, string parameterValue)
    {
        if (!string.IsNullOrWhiteSpace(parameterValue))
        {
            request.AddParameter(parameterName, parameterValue);
        }
    }
    private void AddNonEmptyParameter(RestRequest request, string parameterName, int parameterValue)
    {
        if (parameterValue > 0)
        {
            request.AddParameter(parameterName, parameterValue);
        }
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