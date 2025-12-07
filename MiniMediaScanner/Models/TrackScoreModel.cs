namespace MiniMediaScanner.Models;

public class TrackScoreModel
{
    public Guid MetadataId { get; set; }
    public int ArtistMatchedFor { get; set; }
    public int AlbumMatchedFor { get; set; }
    public int TitleMatchedFor { get; set; }
    public int DurationOffsetBy { get; set; }
    public bool IsrcMatched { get; set; }
    public bool UpcMatched { get; set; }
    public bool DateMatched { get; set; }
    public bool TrackNumberMatched { get; set; }
}