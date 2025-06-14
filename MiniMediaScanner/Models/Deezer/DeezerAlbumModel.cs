using System.Text.Json.Serialization;

namespace MiniMediaScanner.Models.Deezer;

public class DeezerAlbumModel
{
    public long Id { get; set; }
    public string Title { get; set; }
    public string UPC { get; set; }
    public string Link { get; set; }
    public string Label { get; set; }
    
    [JsonPropertyName("explicit_content_lyrics")]
    public int ExplicitContentLyrics { get; set; }
    
    [JsonPropertyName("explicit_content_cover")]
    public int ExplicitContentCover { get; set; }
    
    public string Cover { get; set; }
    
    public string Preview { get; set; }
    
    [JsonPropertyName("nb_tracks")]
    public int NbTracks { get; set; }
    
    public int Duration { get; set; }
    public bool Available { get; set; }
    
    
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
    
    [JsonPropertyName("genre_id")]
    public int GenreId { get; set; }
    
    public int Fans { get; set; }
    
    [JsonPropertyName("release_date")]
    public string ReleaseDate { get; set; }
    
    [JsonPropertyName("record_type")]
    public string RecordType { get; set; }
    
    public string TrackList { get; set; }
    
    [JsonPropertyName("explicit_lyrics")]
    public bool ExplicitLyrics { get; set; }
    
    public string Type { get; set; }
    public DeezerAlbumGenreDataModel Genres { get; set; }
    public List<DeezerAlbumArtistModel> Contributors { get; set; }
    public DeezerAlbumArtistModel Artist { get; set; }
    public DeezerErrorModel? Error { get; set; }
}