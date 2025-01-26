using MiniMediaScanner.Services;

namespace MiniMediaScanner;

public class MetadataInfo
{
    public Guid MetadataId { get; set; }
    public string? Path { get; set; }
    public Guid AlbumId { get; set; }
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

    public string? Tag_Subtitle { get; set; }
    public string? Tag_AlbumSort { get; set; }
    public string? Tag_Comment { get; set; }
    public int Tag_Year { get; set; }
    public int Tag_Track { get; set; }
    public int Tag_TrackCount { get; set; }
    public int Tag_Disc { get; set; }
    public int Tag_DiscCount { get; set; }
    public string? Tag_Lyrics { get; set; }
    public string? Tag_Grouping { get; set; }
    public int Tag_BeatsPerMinute { get; set; }
    public string? Tag_Conductor { get; set; }
    public string? Tag_Copyright { get; set; }
    public DateTime? Tag_DateTagged { get; set; }
    public string? Tag_AmazonId { get; set; }
    public double Tag_ReplayGainTrackGain { get; set; }
    public double Tag_ReplayGainTrackPeak { get; set; }
    public double Tag_ReplayGainAlbumGain { get; set; }
    public double Tag_ReplayGainAlbumPeak { get; set; }
    public string? Tag_InitialKey { get; set; }
    public string? Tag_RemixedBy { get; set; }
    public string? Tag_Publisher { get; set; }
    public string? Tag_ISRC { get; set; }
    public string? Tag_Length { get; set; }
    public string? Tag_AcoustIdFingerPrint { get; set; }
    public float Tag_AcoustIdFingerPrint_Duration { get; set; }
    public string? Tag_AcoustId { get; set; }
    
    public DateTime File_LastWriteTime { get; set; }
    public DateTime File_CreationTime { get; set; }
    public string Tag_AllJsonTags { get; set; }

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
        
        if (string.IsNullOrWhiteSpace(Tag_Subtitle))
        {
            Tag_Subtitle = string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(Tag_AlbumSort))
        {
            Tag_AlbumSort = string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(Tag_Comment))
        {
            Tag_Comment = string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(Tag_Lyrics))
        {
            Tag_Lyrics = string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(Tag_Grouping))
        {
            Tag_Grouping = string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(Tag_Conductor))
        {
            Tag_Conductor = string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(Tag_Copyright))
        {
            Tag_Copyright = string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(Tag_AmazonId))
        {
            Tag_AmazonId = string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(Tag_InitialKey))
        {
            Tag_InitialKey = string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(Tag_RemixedBy))
        {
            Tag_RemixedBy = string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(Tag_Publisher))
        {
            Tag_Publisher = string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(Tag_ISRC))
        {
            Tag_ISRC = string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(Tag_Length))
        {
            Tag_Length = string.Empty;
        }
        
        if (!Tag_DateTagged.HasValue)
        {
            Tag_DateTagged = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }
        
        if (string.IsNullOrWhiteSpace(Tag_AcoustIdFingerPrint))
        {
            Tag_AcoustIdFingerPrint = string.Empty;
        }
        
        if (string.IsNullOrWhiteSpace(Tag_AcoustId))
        {
            Tag_AcoustId = string.Empty;
        }

        var stringNormalizer = new StringNormalizerService();

        Album = stringNormalizer.ReplaceInvalidCharacters(Album);
        Artist = stringNormalizer.ReplaceInvalidCharacters(Artist);
        Title = stringNormalizer.ReplaceInvalidCharacters(Title);
        MusicBrainzArtistId = stringNormalizer.ReplaceInvalidCharacters(MusicBrainzArtistId);
        MusicBrainzDiscId = stringNormalizer.ReplaceInvalidCharacters(MusicBrainzDiscId);
        MusicBrainzReleaseCountry = stringNormalizer.ReplaceInvalidCharacters(MusicBrainzReleaseCountry);
        MusicBrainzReleaseId = stringNormalizer.ReplaceInvalidCharacters(MusicBrainzReleaseId);
        MusicBrainzTrackId = stringNormalizer.ReplaceInvalidCharacters(MusicBrainzTrackId);
        MusicBrainzReleaseStatus = stringNormalizer.ReplaceInvalidCharacters(MusicBrainzReleaseStatus);
        MusicBrainzReleaseType = stringNormalizer.ReplaceInvalidCharacters(MusicBrainzReleaseType);
        MusicBrainzReleaseArtistId = stringNormalizer.ReplaceInvalidCharacters(MusicBrainzReleaseArtistId);
        MusicBrainzReleaseGroupId = stringNormalizer.ReplaceInvalidCharacters(MusicBrainzReleaseGroupId);
        Tag_Subtitle = stringNormalizer.ReplaceInvalidCharacters(Tag_Subtitle);
        Tag_AlbumSort = stringNormalizer.ReplaceInvalidCharacters(Tag_AlbumSort);
        Tag_Comment = stringNormalizer.ReplaceInvalidCharacters(Tag_Comment);
        Tag_Lyrics = stringNormalizer.ReplaceInvalidCharacters(Tag_Lyrics);
        Tag_Grouping = stringNormalizer.ReplaceInvalidCharacters(Tag_Grouping);
        Tag_Conductor = stringNormalizer.ReplaceInvalidCharacters(Tag_Conductor);
        Tag_Copyright = stringNormalizer.ReplaceInvalidCharacters(Tag_Copyright);
        Tag_AmazonId = stringNormalizer.ReplaceInvalidCharacters(Tag_AmazonId);
        Tag_InitialKey = stringNormalizer.ReplaceInvalidCharacters(Tag_InitialKey);
        Tag_RemixedBy = stringNormalizer.ReplaceInvalidCharacters(Tag_RemixedBy);
        Tag_Publisher = stringNormalizer.ReplaceInvalidCharacters(Tag_Publisher);
        Tag_ISRC = stringNormalizer.ReplaceInvalidCharacters(Tag_ISRC);
        Tag_Length = stringNormalizer.ReplaceInvalidCharacters(Tag_Length);
        Tag_AcoustIdFingerPrint = stringNormalizer.ReplaceInvalidCharacters(Tag_AcoustIdFingerPrint);
        Tag_AcoustId = stringNormalizer.ReplaceInvalidCharacters(Tag_AcoustId);
        Tag_AllJsonTags = stringNormalizer.ReplaceInvalidCharacters(Tag_AllJsonTags);
    }
}