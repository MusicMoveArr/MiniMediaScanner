namespace MiniMediaScanner.Models;

public class CoverArtArchiveImageModel
{
    public bool Approved { get; set; }
    public bool Back { get; set; }
    public bool Front { get; set; }
    public List<string> Types { get; set; }
    public CoverArtArchiveImageThumbnailModel Thumbnails { get; set; }
}