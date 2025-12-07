using MiniMediaScanner.Interfaces;
using MiniMediaScanner.Models.Spotify;

namespace MiniMediaScanner.Models;

public class TrackScoreComparerSpotifyModel : ITrackScoreComparerModel
{
    public SpotifyTrackModel TrackModel { get; private set; }

    public TrackScoreComparerSpotifyModel(SpotifyTrackModel trackModel)
    {
        this.TrackModel = trackModel;
    }

    public string Artist => TrackModel.ArtistName;
    public string Album => TrackModel.AlbumName;
    public string AlbumId => TrackModel.AlbumId.ToString();
    public string Title => TrackModel.TrackName;
    public TimeSpan Duration => TrackModel.Duration;
    public string Isrc => TrackModel.Isrc;
    public string Upc => TrackModel.Upc;
    public string Date => TrackModel.ReleaseDate;
    public int TrackNumber => TrackModel.TrackNumber;
    public int TrackTotalCount => TrackModel.TotalTracks;
}