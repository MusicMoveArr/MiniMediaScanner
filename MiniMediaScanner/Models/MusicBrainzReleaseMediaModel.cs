using Newtonsoft.Json;

namespace MiniMediaScanner.Models;

public class MusicBrainzReleaseMediaModel
{
    [JsonProperty(PropertyName = "track-count")]
    public int? TrackCount { get; set; }
    
    public string? Format { get; set; }
    public string? Title { get; set; }
    public int? Position { get; set; }
    
    [JsonProperty(PropertyName = "track-offset")]
    public int? TrackOffset { get; set; }
    
    public List<MusicBrainzReleaseMediaTrackModel>? Tracks { get; set; } = new List<MusicBrainzReleaseMediaTrackModel>();
}