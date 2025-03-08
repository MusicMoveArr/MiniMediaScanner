namespace MiniMediaScanner.Models.Spotify;

public class SpotifyTrackModel
{
    public string TrackName { get; set; }
    public string TrackId { get; set; }
    public string AlbumId { get; set; }
    public string AlbumName { get; set; }
    public int DiscNumber { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Explicit { get; set; }
    public string  TrackHref { get; set; }
    public string TrackNumber { get; set; }
    public string Uri { get; set; }
    public string AlbumGroup { get; set; }
    public string AlbumType { get; set; }
    public string ReleaseDate { get; set; }
    public int TotalTracks { get; set; }
    public string Label { get; set; }
    public string ArtistHref { get; set; }
    public string Genres { get; set; }
    public string ArtistName { get; set; }
    public string ArtistId { get; set; }
}