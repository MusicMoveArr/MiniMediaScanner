namespace MiniMediaScanner;

public class MetadataInfo
{
    public string? MetadataId { get; set; }
    public string? Path { get; set; }
    public string? AlbumId { get; set; }
    public string? Album { get; set; }
    public string? Artist { get; set; }
    public string? Title { get; set; }
    
    public string? MusicBrainzArtistId { get; set; }
    public string? MusicBrainzDiscId { get; set; }
    public string? MusicBrainzReleaseCountry { get; set; }
    public string? MusicBrainzReleaseId { get; set; }
    public string? MusicBrainzTrackId { get; set; }
    public string? MusicBrainzReleaseStatus { get; set; }
    public string? MusicBrainzReleaseType { get; set; }
    public string? MusicBrainzReleaseArtistId { get; set; }
    public string? MusicBrainzReleaseGroupId { get; set; }

    public string? TagSubtitle { get; set; }
    public string? TagAlbumSort { get; set; }
    public string? TagComment { get; set; }
    public int TagYear { get; set; }
    public int TagTrack { get; set; }
    public int TagTrackCount { get; set; }
    public int TagDisc { get; set; }
    public int TagDiscCount { get; set; }
    public string? TagLyrics { get; set; }
    public string? TagGrouping { get; set; }
    public int TagBeatsPerMinute { get; set; }
    public string? TagConductor { get; set; }
    public string? TagCopyright { get; set; }
    public DateTime? TagDateTagged { get; set; }
    public string? TagAmazonId { get; set; }
    public double TagReplayGainTrackGain { get; set; }
    public double TagReplayGainTrackPeak { get; set; }
    public double TagReplayGainAlbumGain { get; set; }
    public double TagReplayGainAlbumPeak { get; set; }
    public string? TagInitialKey { get; set; }
    public string? TagRemixedBy { get; set; }
    public string? TagPublisher { get; set; }
    public string? TagISRC { get; set; }
    public string? TagLength { get; set; }
    public string? TagAcoustIdFingerPrint { get; set; }
    public float TagAcoustIdFingerPrintDuration { get; set; }
    public string? TagAcoustId { get; set; }
    
    public DateTime FileLastWriteTime { get; set; }
    public DateTime FileCreationTime { get; set; }
    public string AllJsonTags { get; set; }

    public void NonNullableValues()
    {
        if (string.IsNullOrWhiteSpace(Album))
        {
            Album = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(Artist))
        {
            Artist = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(Title))
        {
            Title = string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(MusicBrainzArtistId))
        {
            MusicBrainzArtistId = string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(MusicBrainzDiscId))
        {
            MusicBrainzDiscId = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(MusicBrainzReleaseCountry))
        {
            MusicBrainzReleaseCountry = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(MusicBrainzReleaseId))
        {
            MusicBrainzReleaseId = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(MusicBrainzTrackId))
        {
            MusicBrainzTrackId = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(MusicBrainzReleaseStatus))
        {
            MusicBrainzReleaseStatus = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(MusicBrainzReleaseType))
        {
            MusicBrainzReleaseType = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(MusicBrainzReleaseArtistId))
        {
            MusicBrainzReleaseArtistId = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(MusicBrainzReleaseGroupId))
        {
            MusicBrainzReleaseGroupId = string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(TagSubtitle))
        {
            TagSubtitle = string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(TagAlbumSort))
        {
            TagAlbumSort = string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(TagComment))
        {
            TagComment = string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(TagLyrics))
        {
            TagLyrics = string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(TagGrouping))
        {
            TagGrouping = string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(TagConductor))
        {
            TagConductor = string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(TagCopyright))
        {
            TagCopyright = string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(TagAmazonId))
        {
            TagAmazonId = string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(TagInitialKey))
        {
            TagInitialKey = string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(TagRemixedBy))
        {
            TagRemixedBy = string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(TagPublisher))
        {
            TagPublisher = string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(TagISRC))
        {
            TagISRC = string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(TagLength))
        {
            TagLength = string.Empty;
        }
        
        if (!TagDateTagged.HasValue)
        {
            TagDateTagged = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }
        
        if (string.IsNullOrWhiteSpace(TagAcoustIdFingerPrint))
        {
            TagAcoustIdFingerPrint = string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(TagAcoustId))
        {
            TagAcoustId = string.Empty;
        }
    }
}