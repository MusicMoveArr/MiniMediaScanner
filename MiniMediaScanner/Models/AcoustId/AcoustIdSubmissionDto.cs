namespace MiniMediaScanner.Models.AcoustId;

public class AcoustIdSubmissionDto
{
    public int SubmissionId { get; set; }
    public Guid MetadataId { get; set; }
    public string Status { get; set; }
    public DateTime SubmittedAt { get; set; }
}