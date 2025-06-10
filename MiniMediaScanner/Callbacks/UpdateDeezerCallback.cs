using MiniMediaScanner.Callbacks.Status;
using MiniMediaScanner.Models.MusicBrainz;
using SpotifyAPI.Web;

namespace MiniMediaScanner.Callbacks;

public class UpdateDeezerCallback
{
    public long ArtistId { get; set; }
    public string ArtistName { get; set; }
    public string AlbumName { get; set; }
    public int AlbumCount { get; set; }
    public UpdateDeezerStatus Status { get; set; }
    public int Progress { get; set; }
    public string ExtraInfo { get; set; }

    public UpdateDeezerCallback(
        long artistId,
        string artistName,
        string albumName,
        int albumCount,
        UpdateDeezerStatus status,
        int progress,
        string extraInfo = "")
    {
        this.ArtistId = artistId;
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
    
    public UpdateDeezerCallback(
        long artistId,
        UpdateDeezerStatus status)
    {
        this.ArtistId = artistId;
        this.Status = status;
    }
}