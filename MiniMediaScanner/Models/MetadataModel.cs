namespace MiniMediaScanner.Models;

public class MetadataModel
{
    public Guid? MetadataId { get; set; }
    public Guid? ArtistId { get; set; }
    public string? Path { get; set; }
    public string? Title { get; set; }
    public Guid? AlbumId { get; set; }
    public string? ArtistName { get; set; }
    public string? AlbumName { get; set; }
    public string Tag_AllJsonTags { get; set; }
    public int Tag_Track { get; set; }
    public int Tag_TrackCount { get; set; }
    public int Tag_Disc { get; set; }
    public int Tag_DiscCount { get; set; }
    
    public string? Tag_AcoustIdFingerprint { get; set; }
    public string? MusicBrainzArtistId { get; set; }
    public string? Tag_AcoustId { get; set; }
    public string? Tag_Isrc { get; set; }
    public string? Tag_Upc { get; set; }
    public string? Tag_Date { get; set; }
    public TimeSpan TrackLength { get; set; }
}