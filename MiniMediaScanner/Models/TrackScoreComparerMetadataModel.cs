using MiniMediaScanner.Interfaces;
using MiniMediaScanner.Models.Tidal;

namespace MiniMediaScanner.Models;

public class TrackScoreComparerMetadataModel : ITrackScoreComparerModel
{
    public MetadataModel TrackModel { get; private set; }

    public TrackScoreComparerMetadataModel(MetadataModel trackModel)
    {
        this.TrackModel = trackModel;
    }

    public string Artist => TrackModel.ArtistName;
    public string Album => TrackModel.AlbumName;
    public string AlbumId => TrackModel.AlbumId.ToString();
    public string Title => TrackModel.Title;
    public TimeSpan Duration => TrackModel.TrackLength;
    public string Isrc => TrackModel.Tag_Isrc;
    public string Upc => TrackModel.Tag_Upc;
    public string Date => TrackModel.Tag_Date;
    public int TrackNumber => TrackModel.Tag_Track;
    public int TrackTotalCount => TrackModel.Tag_TrackCount;
}