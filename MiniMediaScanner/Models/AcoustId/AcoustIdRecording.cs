using MiniMediaScanner.Models.MusicBrainz;

namespace MiniMediaScanner.Models.AcoustId;

public class AcoustIdRecording
{
    public Guid? Id { get; set; }
    public float? Duration { get; set; }
    public string? Title { get; set; }
    public List<AcoustIdArtists>? Artists { get; set; }
    public List<AcoustIdReleaseGroups> ReleaseGroups { get; set; }
    
    public Guid? AcoustId { get; set; }
    public MusicBrainzArtistReleaseModel? RecordingRelease { get; set; }
}