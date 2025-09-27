namespace MiniMediaScanner.Models.AcoustId;

public class AcoustIdResult
{
    public Guid? Id { get; set; }
    public List<AcoustIdRecording>? Recordings { get; set; }
    public float Score { get; set; }
}