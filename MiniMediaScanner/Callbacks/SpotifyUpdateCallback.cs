using MiniMediaScanner.Callbacks.Status;
using SpotifyAPI.Web;

namespace MiniMediaScanner.Callbacks;

public class SpotifyUpdateCallback
{
    public FullArtist? Artist { get; set; }
    public SimpleAlbum? CurrentAblum { get; set; }
    public List<SimpleAlbum>? SimpleAlbums { get; set; }
    public SpotifyUpdateStatus Status { get; set; }
    public int Progress { get; set; }

    public SpotifyUpdateCallback(
        FullArtist artist, 
        SimpleAlbum currentAblum, 
        List<SimpleAlbum> simpleAlbums,
        SpotifyUpdateStatus status,
        int progress)
    {
        this.Artist = artist;
        this.CurrentAblum = currentAblum;
        this.SimpleAlbums = simpleAlbums;
        this.Status = status;
        this.Progress = progress;
    }
    
    public SpotifyUpdateCallback(
        FullArtist? artist, 
        SpotifyUpdateStatus status)
    {
        this.Artist = artist;
        this.Status = status;
    }
}