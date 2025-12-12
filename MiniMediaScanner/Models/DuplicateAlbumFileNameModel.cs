namespace MiniMediaScanner.Models;

public class DuplicateAlbumFileNameModel
{
    public Guid MetadataId { get; set; }
    public string Path { get; set; }
    public string Title { get; set; }
    public Guid AlbumId { get; set; }
    public string AlbumTitle { get; set; }
    public string FileName { get; set; }
    public string PathWithoutExtension { get; set; }
}