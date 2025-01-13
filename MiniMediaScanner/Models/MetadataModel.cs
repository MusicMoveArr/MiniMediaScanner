namespace MiniMediaScanner.Models;

public class MetadataModel
{
    public string? MetadataId { get; set; }
    public string? Path { get; set; }
    public string? Title { get; set; }
    public string? AlbumId { get; set; }
    public string? ArtistName { get; set; }
    public string? AlbumName { get; set; }
    public int Tag_Track { get; set; }
    public string AllJsonTags { get; set; }
    public int Track { get; set; }
    public int TrackCount { get; set; }
    public int Disc { get; set; }
    public int DiscCount { get; set; }
}