using MiniMediaScanner.Interfaces;

namespace MiniMediaScanner.Models;

public class TrackScoreComparerMetadataModel : ITrackScoreComparerModel
{
    public MetadataModel TrackModel { get; private set; }

    public TrackScoreComparerMetadataModel(MetadataModel trackModel)
    {
        this.TrackModel = trackModel;
    }

    public string Artist => TrackModel.ArtistName ?? string.Empty;
    public string Album => TrackModel.AlbumName ?? string.Empty;
    public string AlbumId => TrackModel.AlbumId?.ToString() ?? Guid.Empty.ToString();
    public string Title => TrackModel.Title ?? string.Empty;
    public TimeSpan Duration => TrackModel.TrackLength;
    public string Isrc => TrackModel.Tag_Isrc ?? string.Empty;
    public string Upc => TrackModel.Tag_Upc ?? string.Empty;
    public string Date => TrackModel.Tag_Date ?? string.Empty;
    public int TrackNumber => TrackModel.Tag_Track;
    public int TrackTotalCount => TrackModel.Tag_TrackCount;
}