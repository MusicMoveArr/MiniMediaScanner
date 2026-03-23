namespace MiniMediaScanner.Models.MusicBrainz;

public class MusicBrainzTrackDbModel
{
    public string TrackName { get; set; }
    public Guid TrackId { get; set; }
    public Guid AlbumId { get; set; }
    public int DiscNumber { get; set; }
    public TimeSpan Duration { get; set; }
    public string TrackHref { get; set; }
    public int TrackPosition { get; set; }
    public string AlbumUPC { get; set; }
    public string AlbumReleaseDate { get; set; }
    public string Label { get; set; }
    public string AlbumName { get; set; }
    public string AlbumHref { get; set; }
    public string ArtistName { get; set; }
    public Guid ArtistId { get; set; }
}