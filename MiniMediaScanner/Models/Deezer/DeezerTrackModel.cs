using System.Text.Json.Serialization;

namespace MiniMediaScanner.Models.Deezer;

public class DeezerTrackModel
{
    public long Id { get; set; }
    public bool Readable { get; set; }
    public string Title { get; set; }
    
    [JsonPropertyName("title_short")]
    public string TitleShort { get; set; }
    
    [JsonPropertyName("title_version")]
    public string? TitleVersion { get; set; }
    
    public string ISRC { get; set; }
    public string Link { get; set; }
    public string Share { get; set; }
    public int Duration { get; set; }
    
    [JsonPropertyName("track_position")]
    public int TrackPosition { get; set; }
    
    [JsonPropertyName("disk_number")]
    public int DiskNumber { get; set; }
    
    public int Rank { get; set; }
    
    [JsonPropertyName("release_date")]
    public string ReleaseDate { get; set; }
    
    [JsonPropertyName("explicit_lyrics")]
    public bool ExplicitLyrics { get; set; }
    
    [JsonPropertyName("explicit_content_lyrics")]
    public int ExplicitContentLyrics { get; set; }
    
    [JsonPropertyName("explicit_content_cover")]
    public int ExplicitContentCover { get; set; }
    
    public string Preview { get; set; }
    public float BPM { get; set; }
    public float Gain { get; set; }
    
    [JsonPropertyName("available_countries")]
    public List<string> AvailableCountries { get; set; }
    
    public List<DeezerTrackContributorModel> Contributors { get; set; }
    
    [JsonPropertyName("md5_image")]
    public string Md5Image { get; set; }
    
    [JsonPropertyName("track_token")]
    public string TrackToken { get; set; }
    
    public DeezerTrackArtistModel Artist { get; set; }
    public DeezerTrackAlbumModel Album { get; set; }
    public string Type { get; set; }
}