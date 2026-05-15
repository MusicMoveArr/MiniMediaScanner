namespace MiniMediaScanner.Models;

public class ArtistMatchModel
{
    public Guid ArtistId { get; set; }
    public string Name { get; set; }
    public int TrackCount { get; set; }
}