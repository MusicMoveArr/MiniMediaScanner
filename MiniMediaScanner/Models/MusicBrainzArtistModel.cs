using System.Text.Json.Serialization;
using MiniMediaScanner.Models.MusicBrainzRecordings;

namespace MiniMediaScanner.Models;

public class MusicBrainzArtistModel
{
    [JsonPropertyName("release-count")]
    public int? ReleaseCount { get; set; }
    
    [JsonPropertyName("artist-credit")]
    public List<MusicBrainzArtistCreditModel> ArtistCredit { get; set; } = new List<MusicBrainzArtistCreditModel>();
    
    public List<MusicBrainzArtistReleaseModel> Releases { get; set; } = new List<MusicBrainzArtistReleaseModel>();
}