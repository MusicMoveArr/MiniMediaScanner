using System.Text.Json.Serialization;

namespace MiniMediaScanner.Models;

public class MusicBrainzArtistModel
{
    [JsonPropertyName("release-count")]
    public int ReleaseCount { get; set; }
    public List<MusicBrainzArtistReleaseModel> Releases { get; set; } = new List<MusicBrainzArtistReleaseModel>();
}