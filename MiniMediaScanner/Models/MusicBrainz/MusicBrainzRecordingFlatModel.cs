namespace MiniMediaScanner.Models.MusicBrainz;

public class MusicBrainzRecordingFlatModel
{
    public int ArtistReleaseCount { get; set; }
    public string ArtistDisambiguation { get; set; }
    public string ArtistName { get; set; }
    public Guid ArtistId { get; set; }
    public string ArtistSortName { get; set; }
    public string ArtistType { get; set; }
    public string ArtistCountry { get; set; }
    
    public Guid ReleaseId { get; set; }
    public string ReleaseTitle { get; set; }
    public string ReleaseStatus { get; set; }
    public string ReleaseStatusId { get; set; }
    public string ReleaseDate { get; set; }
    public string ReleaseBarcode { get; set; }
    public string ReleaseCountry { get; set; }
    public string ReleaseDisambiguation { get; set; }
    public string ReleaseQuality { get; set; }
    public string ReleaseCatalogNumber { get; set; }
    
    
    public int ReleaseTrackMediaTrackCount { get; set; }
    public string ReleaseTrackMediaFormat { get; set; }
    public string ReleaseTrackTitle { get; set; }
    public string ReleaseTrackRecordingTitle { get; set; }
    public int ReleaseTrackPosition { get; set; }
    public int ReleaseTrackMediaTrackOffset { get; set; }
    public Guid ReleaseTrackId { get; set; }
    public int ReleaseTrackLength { get; set; }
    public int ReleaseTrackNumber { get; set; }
    public bool ReleaseTrackRecordingVideo { get; set; }
    public Guid ReleaseTrackRecordingId { get; set; }
    public int ReleaseTrackDiscNumber { get; set; }
    
    
    public int TrackArtistReleaseCount { get; set; }
    public string TrackArtistDisambiguation { get; set; }
    public string TrackArtistName { get; set; }
    public Guid TrackArtistId { get; set; }
    public string TrackArtistSortName { get; set; }
    public string TrackArtistType { get; set; }
    public string TrackArtistCountry { get; set; }
    public string TrackArtistJoinPhrase { get; set; }
    public int TrackArtistIndex { get; set; }
    
    public Guid? LabelId { get; set; }
    public Guid? AreaId { get; set; }
    public string? LabelName { get; set; }
    public int? LabelCode { get; set; }
    public string? LabelCountry { get; set; }
}