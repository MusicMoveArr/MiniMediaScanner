namespace MiniMediaScanner.Models.AcoustId;

public class AcoustIdCheckSubmission
{
    public int Id { get; set; }
    public string Status { get; set; }
    public AcoustIdCheckSubmissionResult Result { get; set; }
}