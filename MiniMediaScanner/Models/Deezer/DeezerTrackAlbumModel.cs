using System.Text.Json.Serialization;

namespace MiniMediaScanner.Models.Deezer;

public class DeezerTrackAlbumModel
{
    public long Id { get; set; }
    public string Title { get; set; }
    public string Link { get; set; }
    public string Cover { get; set; }
    
    [JsonPropertyName("cover_small")]
    public string CoverSmall { get; set; }
    
    [JsonPropertyName("cover_medium")]
    public string CoverMedium { get; set; }
    
    [JsonPropertyName("cover_big")]
    public string CoverBig { get; set; }
    
    [JsonPropertyName("cover_xl")]
    public string CoverXL { get; set; }
    
    [JsonPropertyName("md5_image")]
    public string Md5Image { get; set; }
    
    [JsonPropertyName("release_date")]
    public string ReleaseDate { get; set; }
    
    public string TrackList { get; set; }
    
    public string Type { get; set; }
}