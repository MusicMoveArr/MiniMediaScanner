using MiniMediaScanner.Helpers;

namespace MiniMediaScanner;

public class MetadataInfo
{
    private const int MaxFilePartNameLength = 80;
    public const string VariousArtistsName = "Various Artists";
    
    public Guid MetadataId { get; set; }
    public string? Path { get; set; }
    public Guid AlbumId { get; set; }
    public Guid ArtistId { get; set; }
    public string? Album { get; set; }
    public string? Artist { get; set; }
    public string? Title { get; set; }

    public string? ArtistName => Artist;
    public string? AlbumName => Album;
    
    public string? MusicBrainzArtistId { get; set; }
    public string? MusicBrainzDiscId { get; set; }
    public string? MusicBrainzReleaseCountry { get; set; }
    public string? MusicBrainzReleaseId { get; set; }
    public string? MusicBrainzTrackId { get; set; }
    public string? MusicBrainzReleaseStatus { get; set; }
    public string? MusicBrainzReleaseType { get; set; }
    public string? MusicBrainzReleaseArtistId { get; set; }
    public string? MusicBrainzReleaseGroupId { get; set; }
    
    
    public long? DeezerArtistId { get; set; }
    public long? DiscogsArtistId { get; set; }
    public string? SpotifyArtistId { get; set; }
    public int? TidalArtistId { get; set; }
    public long? SoundcloudArtistId { get; set; }

    public int Tag_Year { get; set; }
    public int Tag_Track { get; set; }
    public int Tag_TrackCount { get; set; }
    public int Tag_Disc { get; set; }
    public int Tag_DiscCount { get; set; }
    public DateTime? Tag_DateTagged { get; set; }
    public string? Tag_ISRC { get; set; }
    public string? Tag_Length { get; set; }
    public string? Tag_AcoustIdFingerPrint { get; set; }
    public float Tag_AcoustIdFingerPrint_Duration { get; set; }
    public string? Tag_AcoustId { get; set; }
    
    public DateTime File_LastWriteTime { get; set; }
    public DateTime File_CreationTime { get; set; }
    public long File_Size { get; set; }
    
    public string Tag_AllJsonTags { get; set; }
    public Dictionary<string, string> MediaTags { get; set; }

    public void NonNullableValues()
    {
        if (!Tag_DateTagged.HasValue)
        {
            Tag_DateTagged = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        Album = StringHelper.CleanupInvalidChars(Album);
        Artist = StringHelper.CleanupInvalidChars(Artist);
        Title = StringHelper.CleanupInvalidChars(Title);
        MusicBrainzArtistId = StringHelper.CleanupInvalidChars(MusicBrainzArtistId);
        MusicBrainzDiscId = StringHelper.CleanupInvalidChars(MusicBrainzDiscId);
        MusicBrainzReleaseCountry = StringHelper.CleanupInvalidChars(MusicBrainzReleaseCountry);
        MusicBrainzReleaseId = StringHelper.CleanupInvalidChars(MusicBrainzReleaseId);
        MusicBrainzTrackId = StringHelper.CleanupInvalidChars(MusicBrainzTrackId);
        MusicBrainzReleaseStatus = StringHelper.CleanupInvalidChars(MusicBrainzReleaseStatus);
        MusicBrainzReleaseType = StringHelper.CleanupInvalidChars(MusicBrainzReleaseType);
        MusicBrainzReleaseArtistId = StringHelper.CleanupInvalidChars(MusicBrainzReleaseArtistId);
        MusicBrainzReleaseGroupId = StringHelper.CleanupInvalidChars(MusicBrainzReleaseGroupId);
        Tag_ISRC = StringHelper.CleanupInvalidChars(Tag_ISRC);
        Tag_Length = StringHelper.CleanupInvalidChars(Tag_Length);
        Tag_AcoustIdFingerPrint = StringHelper.CleanupInvalidChars(Tag_AcoustIdFingerPrint);
        Tag_AcoustId = StringHelper.CleanupInvalidChars(Tag_AcoustId);
        Tag_AllJsonTags = StringHelper.CleanupInvalidChars(Tag_AllJsonTags);
    }
    
    public string CleanArtist
    {
        get
        {
            string? artist = ArtistName;
            
            if (string.IsNullOrWhiteSpace(artist) ||
                (artist.Contains(VariousArtistsName) && !string.IsNullOrWhiteSpace(ArtistName)))
            {
                artist = ArtistName;
            }
            
            artist = ArtistHelper.GetUncoupledArtistName(artist);
            artist = ArtistHelper.GetShortWordVersion(artist, MaxFilePartNameLength);
            return artist
                .Replace('/', '+')
                .Replace('\\', '+');
        }
    }

    
    public string CleanAlbum
    {
        get
        {
            string albumName = ArtistHelper.GetShortWordVersion(AlbumName, MaxFilePartNameLength);
            return albumName
                .Replace('/', '+')
                .Replace('\\', '+');
        }
    }
    public string CleanArtistUpper => CleanArtist.ToUpper();
    public string CleanAlbumUpper => CleanAlbum.ToUpper();
    public string ArtistUpper => ArtistName.ToUpper();
    public string AlbumUpper => AlbumName.ToUpper();
}