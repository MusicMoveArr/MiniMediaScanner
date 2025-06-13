namespace MiniMediaScanner.Models.Deezer;

public class DeezerTrackDbModel
{
    public string TrackName { get; set; }
    public long TrackId { get; set; }
    public long AlbumId { get; set; }
    public int DiscNumber { get; set; }
    public TimeSpan Duration { get; set; }
    public bool ExplicitLyrics { get; set; }
    public string TrackHref { get; set; }
    public int TrackPosition { get; set; }
    public string TrackISRC { get; set; }
    public string AlbumUPC { get; set; }
    public string AlbumReleaseDate { get; set; }
    public int AlbumTotalTracks { get; set; }
    public string Label { get; set; }
    public string AlbumName { get; set; }
    public string AlbumHref { get; set; }
    public string ArtistHref { get; set; }
    public string ArtistName { get; set; }
    public long ArtistId { get; set; }
}