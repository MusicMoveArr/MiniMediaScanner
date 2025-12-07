using MiniMediaScanner.Interfaces;

namespace MiniMediaScanner.Models;

public class TrackScoreTargetModel : ITrackScoreComparerModel
{
    public required Guid MetadataId { get; init; }
    public required string Artist { get; init; }
    public required string Album { get; init; }
    public required string AlbumId { get; init; }
    public required string Title { get; init; }
    public required TimeSpan Duration { get; init; }
    public required string Isrc { get; init; }
    public required string Upc { get; init; }
    public required string Date { get; init; }
    public required int TrackNumber { get; init; }
    public required int TrackTotalCount { get; init; }
}