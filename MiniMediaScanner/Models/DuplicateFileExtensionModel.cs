namespace MiniMediaScanner.Models;

public class DuplicateFileExtensionModel
{
    public Guid MetadataId { get; set; }
    public string Path { get; set; }
    public string Title { get; set; }
    public Guid AlbumId { get; set; }
    public string FilePathWithoutExtension { get; set; }
}