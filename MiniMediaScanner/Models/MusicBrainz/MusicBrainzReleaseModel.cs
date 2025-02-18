namespace MiniMediaScanner.Models.MusicBrainz;

public class MusicBrainzReleaseModel
{
    public string? Title { get; set; }
    public string? Status { get; set; }
    
    public List<MusicBrainzReleaseMediaModel>? Media { get; set; }
}