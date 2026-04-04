using MiniMediaScanner.Callbacks.Status;
using MiniMediaScanner.Models.MusicBrainz;
using SpotifyAPI.Web;

namespace MiniMediaScanner.Callbacks;

public class UpdateSoundCloudCallback
{
    public long UserId { get; set; }
    public string UserName { get; set; }
    public string PlaylistName { get; set; }
    public int PlaylistCount { get; set; }
    public UpdateSoundCloudStatus Status { get; set; }
    public int Progress { get; set; }
    public string ExtraInfo { get; set; }

    public UpdateSoundCloudCallback(
        long userId,
        string userName,
        string playlistName,
        int playlistCount,
        UpdateSoundCloudStatus status,
        int progress,
        string extraInfo = "")
    {
        this.UserId = userId;
        this.UserName = userName;
        this.PlaylistName = playlistName;
        this.PlaylistCount = playlistCount;
        this.Status = status;
        this.Progress = progress;

        if (extraInfo?.Length > 0)
        {
            extraInfo = ", " + extraInfo;
        }
        this.ExtraInfo = extraInfo;
    }
    
    public UpdateSoundCloudCallback(
        long userId,
        UpdateSoundCloudStatus status)
    {
        this.UserId = userId;
        this.Status = status;
    }
}