using Newtonsoft.Json;

namespace MiniMediaScanner.Models;

public class MusicBrainzReleaseMediaModel
{
    [JsonProperty(PropertyName = "track-count")]
    public int TrackCount { get; set; }
    public string? Format { get; set; }
    public string? Title { get; set; }
    public List<MusicBrainzReleaseMediaTrackModel>? Tracks { get; set; }
}