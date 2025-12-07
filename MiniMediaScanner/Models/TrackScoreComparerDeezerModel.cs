using MiniMediaScanner.Interfaces;
using MiniMediaScanner.Models.Deezer;

namespace MiniMediaScanner.Models;

public class TrackScoreComparerDeezerModel : ITrackScoreComparerModel
{
    public DeezerTrackDbModel TrackModel { get; private set; }

    public TrackScoreComparerDeezerModel(DeezerTrackDbModel trackModel)
    {
        this.TrackModel = trackModel;
    }

    public string Artist => TrackModel.ArtistName;
    public string Album => TrackModel.AlbumName;
    public string AlbumId => TrackModel.AlbumId.ToString();
    public string Title => TrackModel.TrackName;
    public TimeSpan Duration => TrackModel.Duration;
    public string Isrc => TrackModel.TrackISRC;
    public string Upc => TrackModel.AlbumUPC;
    public string Date => TrackModel.AlbumReleaseDate;
    public int TrackNumber => TrackModel.TrackPosition;
    public int TrackTotalCount => TrackModel.AlbumTotalTracks;
}