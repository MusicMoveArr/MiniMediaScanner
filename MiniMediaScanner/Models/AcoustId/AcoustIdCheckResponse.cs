namespace MiniMediaScanner.Models.AcoustId;

public class AcoustIdCheckResponse
{
    public string Status { get; set; }
    public List<AcoustIdCheckSubmission> Submissions { get; set; }
}