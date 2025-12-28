namespace MiniMediaScanner.Models.Discogs;

public class DiscogsReleaseModel
{
    public int Id { get; set; }
    public string title { get; set; }
    public string Data_Quality { get; set; }
    public List<DiscogsReleaseImageModel> Images { get; set; }
}