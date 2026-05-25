using MiniMediaScanner.Callbacks.Status;
using MiniMediaScanner.Models.MusicBrainz;
using SpotifyAPI.Web;

namespace MiniMediaScanner.Callbacks;

public class UpdateLastFmCallback
{
    public string ArtistName { get; set; }
    public string AlbumName { get; set; }
    public int AlbumCount { get; set; }
    public UpdateLastFmStatus Status { get; set; }
    public int Progress { get; set; }
    public string ExtraInfo { get; set; }

    public UpdateLastFmCallback(
        string artistName,
        string albumName,
        int albumCount,
        UpdateLastFmStatus status,
        int progress,
        string extraInfo = "")
    {
        this.ArtistName = artistName;
        this.AlbumName = albumName;
        this.AlbumCount = albumCount;
        this.Status = status;
        this.Progress = progress;

        if (extraInfo?.Length > 0)
        {
            extraInfo = ", " + extraInfo;
        }
        this.ExtraInfo = extraInfo;
    }
    
    public UpdateLastFmCallback(
        string artistName,
        UpdateLastFmStatus status)
    {
        this.ArtistName = artistName;
        this.Status = status;
    }
}