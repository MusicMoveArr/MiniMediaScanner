namespace MiniMediaScanner.Models;

public class CanUpdateMetadataModel
{
    public Guid MetadataId { get; set; }
    public string Path { get; set; }
    public DateTime? File_LastWriteTime { get; set; }
    public DateTime? File_CreationTime { get; set; }
}