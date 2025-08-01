using System.Text.Json.Serialization;

namespace MiniMediaScanner.Models.Deezer;

public class DeezerAlbumArtistModel
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Link { get; set; }
    public string Share { get; set; }
    public string Picture { get; set; }
    
    [JsonPropertyName("picture_small")]
    public string PictureSmall { get; set; }
    
    [JsonPropertyName("picture_medium")]
    public string PictureMedium { get; set; }
    
    [JsonPropertyName("picture_big")]
    public string PictureBig { get; set; }
    
    [JsonPropertyName("picture_xl")]
    public string PictureXL { get; set; }
    
    public bool Radio { get; set; }
    public string TrackList { get; set; }
    public string Type { get; set; }
    public string Role { get; set; }
}