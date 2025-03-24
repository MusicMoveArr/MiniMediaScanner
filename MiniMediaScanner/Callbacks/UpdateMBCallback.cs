using MiniMediaScanner.Callbacks.Status;
using MiniMediaScanner.Models.MusicBrainz;
using SpotifyAPI.Web;

namespace MiniMediaScanner.Callbacks;

public class UpdateMBCallback
{
    public Guid? ArtistId { get; set; }
    public MusicBrainzArtistInfoModel? Artist { get; set; }
    public MusicBrainzArtistReleaseModel? CurrentAlbum { get; set; }
    public List<MusicBrainzArtistReleaseModel>? Albums { get; set; }
    public UpdateMBStatus Status { get; set; }
    public int Progress { get; set; }

    public UpdateMBCallback(
        Guid artistId,
        MusicBrainzArtistInfoModel artist, 
        MusicBrainzArtistReleaseModel currentAlbum, 
        List<MusicBrainzArtistReleaseModel> albums,
        UpdateMBStatus status,
        int progress)
    {
        this.ArtistId = artistId;
        this.Artist = artist;
        this.CurrentAlbum = currentAlbum;
        this.Albums = albums;
        this.Status = status;
        this.Progress = progress;
    }
    
    public UpdateMBCallback(
        Guid artistId,
        UpdateMBStatus status)
    {
        this.ArtistId = artistId;
        this.Status = status;
    }
}