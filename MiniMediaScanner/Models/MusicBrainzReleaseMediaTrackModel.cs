namespace MiniMediaScanner.Models;

public class MusicBrainzReleaseMediaTrackModel
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public int? Length { get; set; }
    public int? Number { get; set; }
    public int? Position { get; set; }
    public MusicBrainzReleaseMediaTrackRecordingModel? Recording { get; set; }
}