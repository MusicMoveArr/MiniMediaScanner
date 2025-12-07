using MiniMediaScanner.Helpers;
using MiniMediaScanner.Interfaces;
using MiniMediaScanner.Models;

namespace MiniMediaScanner.Services;

public class TrackScoreService
{
    public List<TrackScoreResult> GetAllTrackScore(
        List<TrackScoreTargetModel> targets, 
        IEnumerable<ITrackScoreComparerModel> trackList,
        int minimumMatchPercentage)
    {
        var results = targets
            .Select(target => 
                GetTrackScores(target,
                    trackList,
                    minimumMatchPercentage)
            .FirstOrDefault())
            .Where(match => match is not null)
            .ToList();
        
        return results!;
    }
    
    public TrackScoreResult? GetFirstTrackScore(
        TrackScoreTargetModel target, 
        IEnumerable<ITrackScoreComparerModel> trackList,
        int minimumMatchPercentage)
    {
        return GetTrackScores(target,
            trackList,
            minimumMatchPercentage)
            .FirstOrDefault();
    }
    
    public List<TrackScoreResult> GetTrackScores(
        TrackScoreTargetModel target, 
        IEnumerable<ITrackScoreComparerModel> trackList,
        int minimumMatchPercentage)
    {
        var matches = trackList
            .Select(track => new TrackScoreResult
            {
                TrackScoreComparer = track,
                TrackScore = new TrackScoreModel
                {
                    MetadataId = target.MetadataId,
                    ArtistMatchedFor = FuzzyHelper.FuzzRatioToLower(target.Artist, track.Artist),
                    AlbumMatchedFor = FuzzyHelper.FuzzRatioToLower(target.Album, track.Album),
                    TitleMatchedFor = FuzzyHelper.FuzzRatioToLower(target.Title, track.Title),
                    DurationOffsetBy = Math.Abs(target.Duration.Seconds - track.Duration.Seconds),
                    IsrcMatched = string.Equals(target.Isrc, track.Isrc),
                    UpcMatched = string.Equals(target.Upc, track.Upc),
                    TrackNumberMatched = target.TrackNumber == track.TrackNumber,
                    DateMatched = target.Date.Length == 4 ? 
                        track.Date.Contains(target.Date) : 
                        string.Equals(target.Date, track.Date),
                }
            })
            .Where(match => match.TrackScore.ArtistMatchedFor >= minimumMatchPercentage)
            .Where(match => match.TrackScore.AlbumMatchedFor >= minimumMatchPercentage)
            .Where(match => match.TrackScore.TitleMatchedFor >= minimumMatchPercentage)
            .Where(match => FuzzyHelper.ExactNumberMatch(target.Artist, match.TrackScoreComparer.Artist))
            .Where(match => FuzzyHelper.ExactNumberMatch(target.Album, match.TrackScoreComparer.Album))
            .Where(match => FuzzyHelper.ExactNumberMatch(target.Title, match.TrackScoreComparer.Title))
            .Where(match => match.TrackScore.TitleMatchedFor >= minimumMatchPercentage)
            .Where(match => match.TrackScore.TitleMatchedFor >= minimumMatchPercentage)
            .OrderByDescending(match => match.TrackScore.ArtistMatchedFor)
            .ThenByDescending(match => match.TrackScore.AlbumMatchedFor)
            .ThenByDescending(match => match.TrackScore.TitleMatchedFor)
            .ThenBy(match =>  match.TrackScore.DurationOffsetBy)
            .ToList();

        if (!matches.Any())
        {
            
        }
        if (matches.Any(match => match.TrackScore.DateMatched ||
                                 match.TrackScore.IsrcMatched ||
                                 match.TrackScore.UpcMatched))
        {
            matches = matches
                .Where(match => 
                                match.TrackScore.DateMatched ||
                                match.TrackScore.IsrcMatched ||
                                match.TrackScore.UpcMatched)
                .ToList();
        }

        return matches;
    }
}