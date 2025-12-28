namespace MiniMediaScanner.Models.Discogs;

public class DiscogsArtistModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Resource_Url { get; set; }
    public string Uri { get; set; }
    public string Releases_Url { get; set; }
    public string Profile { get; set; }
    public List<string> Urls { get; set; }
    public List<string> Namevariations { get; set; }
    public string Data_Quality { get; set; }
    
    public List<DiscogsArtistImageModel> Images { get; set; }
}