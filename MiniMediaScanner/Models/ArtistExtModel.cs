namespace MiniMediaScanner.Models;

public class ArtistExtModel
{
    public Guid? ArtistId { get; set; }
    public string ExtArtistId { get; set; }
    public string Provider { get; set; }

    public ArtistExtModel()
    {
        
    }

    public ArtistExtModel(Guid artistId, string extArtistId, string provider)
    {
        this.ArtistId = artistId;
        this.ExtArtistId = extArtistId;
        this.Provider = provider;
    }
}