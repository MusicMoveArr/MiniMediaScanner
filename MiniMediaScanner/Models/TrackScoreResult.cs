using MiniMediaScanner.Interfaces;

namespace MiniMediaScanner.Models;

public class TrackScoreResult
{
    public required ITrackScoreComparerModel TrackScoreComparer { get; init; }
    public required TrackScoreModel TrackScore { get; init; }
}