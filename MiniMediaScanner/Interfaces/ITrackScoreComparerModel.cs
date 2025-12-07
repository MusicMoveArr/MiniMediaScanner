namespace MiniMediaScanner.Interfaces;

public interface ITrackScoreComparerModel
{
    string Artist { get; }
    string Album { get; }
    string AlbumId { get; }
    string Title { get; }
    TimeSpan Duration { get; }
    string Isrc { get; }
    string Upc { get; }
    string Date { get; }
    int TrackNumber { get; }
    int TrackTotalCount { get; }
}