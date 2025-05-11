using System.Text.Json.Serialization;

namespace MiniMediaScanner.Models.MusicBrainz;

public class MusicBrainzLabelInfoLabelAreaModel
{
    public string Name { get; set; }
    public string? Disambiguation { get; set; }
    public string Id { get; set; }
    
    public string? Type { get; set; }
    
    [JsonPropertyName("sort-name")]
    public string? SortName { get; set; }
    
    [JsonPropertyName("type-id")]
    public string? TypeId { get; set; }
}