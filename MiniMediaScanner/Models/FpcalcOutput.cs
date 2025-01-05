using Newtonsoft.Json;

namespace MiniMediaScanner.Models;

public class FpcalcOutput
{
    [JsonProperty("duration")]
    public float Duration { get; set; }
    
    [JsonProperty("fingerprint")]
    public string? Fingerprint { get; set; }
}