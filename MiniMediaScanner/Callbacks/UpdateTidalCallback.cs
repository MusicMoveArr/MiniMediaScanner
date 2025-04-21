using MiniMediaScanner.Callbacks.Status;
using MiniMediaScanner.Models.MusicBrainz;
using SpotifyAPI.Web;

namespace MiniMediaScanner.Callbacks;

public class UpdateTidalCallback
{
    public int ArtistId { get; set; }
    public string ArtistName { get; set; }
    public string AlbumName { get; set; }
    public int AlbumCount { get; set; }
    public UpdateTidalStatus Status { get; set; }
    public int Progress { get; set; }
    public string ExtraInfo { get; set; }

    public UpdateTidalCallback(
        int artistId,
        string artistName,
        string albumName,
        int albumCount,
        UpdateTidalStatus status,
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
    
    public UpdateTidalCallback(
        int artistId,
        UpdateTidalStatus status)
    {
        this.ArtistId = artistId;
        this.Status = status;
    }
}