namespace MiniMediaScanner.Models.MusicBrainz;

public class MusicBrainzRecordingFlatModel
{
    public int ArtistReleaseCount { get; set; }
    public string ArtistDisambiguation { get; set; }
    public string ArtistName { get; set; }
    public string ArtistMusicBrainzRemoteId { get; set; }
    public string ArtistSortName { get; set; }
    public string ArtistType { get; set; }
    
    public string ReleaseMusicBrainzRemoteReleaseId { get; set; }
    public string ReleaseTitle { get; set; }
    public string ReleaseStatus { get; set; }
    public string ReleaseStatusId { get; set; }
    public DateTime ReleaseDate { get; set; }
    public string ReleaseBarcode { get; set; }
    public string ReleaseCountry { get; set; }
    public string ReleaseDisambiguation { get; set; }
    public string ReleaseQuality { get; set; }
    
    public int ReleaseTrackMediaTrackCount { get; set; }
    public string ReleaseTrackMediaFormat { get; set; }
    public string ReleaseTrackTitle { get; set; }
    public int ReleaseTrackPosition { get; set; }
    public int ReleaseTrackMediaTrackOffset { get; set; }
    public string ReleaseTrackMusicBrainzRemoteReleaseTrackId { get; set; }
    public int ReleaseTrackLength { get; set; }
    public int ReleaseTrackNumber { get; set; }
    public bool ReleaseTrackRecordingVideo { get; set; }
    public string ReleaseTrackRecordingId { get; set; }
}