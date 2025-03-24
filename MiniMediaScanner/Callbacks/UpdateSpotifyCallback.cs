using MiniMediaScanner.Callbacks.Status;
using SpotifyAPI.Web;

namespace MiniMediaScanner.Callbacks;

public class UpdateSpotifyCallback
{
    public FullArtist? Artist { get; set; }
    public SimpleAlbum? CurrentAblum { get; set; }
    public List<SimpleAlbum>? Albums { get; set; }
    public UpdateSpotifyStatus Status { get; set; }
    public int Progress { get; set; }

    public UpdateSpotifyCallback(
        FullArtist artist, 
        SimpleAlbum currentAblum, 
        List<SimpleAlbum> albums,
        UpdateSpotifyStatus status,
        int progress)
    {
        this.Artist = artist;
        this.CurrentAblum = currentAblum;
        this.Albums = albums;
        this.Status = status;
        this.Progress = progress;
    }
    
    public UpdateSpotifyCallback(
        FullArtist? artist, 
        UpdateSpotifyStatus status)
    {
        this.Artist = artist;
        this.Status = status;
    }
}