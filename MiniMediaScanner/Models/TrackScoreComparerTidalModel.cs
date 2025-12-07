using MiniMediaScanner.Interfaces;
using MiniMediaScanner.Models.Tidal;

namespace MiniMediaScanner.Models;

public class TrackScoreComparerTidalModel : ITrackScoreComparerModel
{
    public TidalTrackModel TrackModel { get; private set; }

    public TrackScoreComparerTidalModel(TidalTrackModel trackModel)
    {
        this.TrackModel = trackModel;
    }

    public string Artist => TrackModel.ArtistName;
    public string Album => TrackModel.AlbumName;
    public string AlbumId => TrackModel.AlbumId.ToString();
    public string Title => TrackModel.FullTrackName;
    public TimeSpan Duration => TimeSpan.Parse(TrackModel.Duration);
    public string Isrc => TrackModel.TrackISRC;
    public string Upc => TrackModel.AlbumUPC;
    public string Date => TrackModel.ReleaseDate;
    public int TrackNumber => TrackModel.TrackNumber;
    public int TrackTotalCount => TrackModel.TotalTracks;
}