using System.Text.Json.Serialization;

namespace MiniMediaScanner.Models.MusicBrainz;

public class MusicBrainzLabelInfoModel
{
    [JsonPropertyName("catalog-number")]
    public string? CataLogNumber { get; set; }
    public MusicBrainzLabelInfoLabelModel? Label { get; set; }
}