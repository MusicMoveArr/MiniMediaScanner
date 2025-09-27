using System.Text.Json.Serialization;

namespace MiniMediaScanner.Models.AcoustId;

public class AcoustIdMediums
{
    public int Position { get; set; }
    
    [JsonPropertyName("track_count")]
    public int TrackCount { get; set; }
    
    public List<AcoustIdTrack> Tracks { get; set; }
}