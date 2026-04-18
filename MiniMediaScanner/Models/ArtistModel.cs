namespace MiniMediaScanner.Models;

public class ArtistModel
{
    public Guid ArtistId { get; set; }
    public string Name { get; set; }
    public List<ArtistExtModel> ExtArtists { get; set; } = new List<ArtistExtModel>();
}