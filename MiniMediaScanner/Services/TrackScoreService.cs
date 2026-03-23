using MiniMediaScanner.Helpers;
using MiniMediaScanner.Interfaces;
using MiniMediaScanner.Models;

namespace MiniMediaScanner.Services;

public class TrackScoreService
{
    //is used for preventing mismatching
    //e.g., Tidal can have "(Album Version)" whilst Deezer has "" as versioning
    //mismatching won't happen regardless, we filter as well on track length/ISRC/UPC
    //of course there are some versions we just cannot remove easily like "Instrumental" and such
    public readonly string[] _versionIgnore =
    [
        "Original Mix",
        "Album Version",
        "Radio Edit",
        "Extended Mix"
    ];
    
    public List<TrackScoreResult> GetAllTrackScore(
        List<TrackScoreTargetModel> targets, 
        IEnumerable<ITrackScoreComparerModel> trackList,
        int minimumMatchPercentage,
        bool ignoreTrackVersion = false)
    {
        var results = targets
            .Select(target => 
                GetTrackScores(target,
                    trackList,
                    minimumMatchPercentage,
                    ignoreTrackVersion)
            .FirstOrDefault())
            .Where(match => match is not null)
            .ToList();
        return results!;
    }
    
    public TrackScoreResult? GetFirstTrackScore(
        TrackScoreTargetModel target, 
        IEnumerable<ITrackScoreComparerModel> trackList,
        int minimumMatchPercentage,
        bool ignoreTrackVersion = false)
    {
        return GetTrackScores(target,
            trackList,
            minimumMatchPercentage,
            ignoreTrackVersion)
            .FirstOrDefault();
    }
    
    public List<TrackScoreResult> GetTrackScores(
        TrackScoreTargetModel target, 
        IEnumerable<ITrackScoreComparerModel> trackList,
        int minimumMatchPercentage,
        bool ignoreTrackVersion = false)
    {
        var matches = trackList
            .Select(track => new TrackScoreResult
            {
                TrackScoreComparer = track,
                TrackScore = new TrackScoreModel
                {
                    MetadataId = target.MetadataId,
                    ArtistMatchedFor = FuzzyHelper.FuzzRatioToLower(track.Artist, target.Artist),
                    AlbumMatchedFor = FuzzyHelper.FuzzRatioToLower(track.Album, target.Album),
                    TitleMatchedFor = ignoreTrackVersion ?
                        FuzzyHelper.FuzzRatioToLower(GetTrackVersionIgnore(track.Title), GetTrackVersionIgnore(target.Title)) :
                        FuzzyHelper.FuzzRatioToLower(track.Title, target.Title),
                    DurationOffsetBy = Math.Abs(target.Duration.Seconds - track.Duration.Seconds),
                    IsrcMatched = string.Equals(target.Isrc, track.Isrc),
                    UpcMatched = string.Equals(target.Upc, track.Upc),
                    DateMatched = target.Date?.Length == 4 ? 
                        track.Date.Contains(target.Date) : 
                        string.Equals(target.Date, track.Date),
                }
            })
            .Where(match => match.TrackScore.ArtistMatchedFor >= minimumMatchPercentage)
            .Where(match => match.TrackScore.AlbumMatchedFor >= minimumMatchPercentage)
            .Where(match => match.TrackScore.TitleMatchedFor >= minimumMatchPercentage)
            .Where(match => match.TrackScore.DurationOffsetBy is > -5 and < 5)
            .Where(match => FuzzyHelper.ExactNumberMatch(target.Artist, match.TrackScoreComparer.Artist))
            .Where(match => FuzzyHelper.ExactNumberMatch(target.Album, match.TrackScoreComparer.Album))
            .Where(match => FuzzyHelper.ExactNumberMatch(target.Title, match.TrackScoreComparer.Title))
            .OrderByDescending(match => match.TrackScore.ArtistMatchedFor)
            .ThenByDescending(match => match.TrackScore.AlbumMatchedFor)
            .ThenByDescending(match => match.TrackScore.TitleMatchedFor)
            .ThenBy(match =>  match.TrackScore.DurationOffsetBy)
            .ToList();

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

    private string GetTrackVersionIgnore(string title)
    {
        foreach (var version in _versionIgnore)
        {
            title = title.Replace(version, string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        title = title.Replace("()", string.Empty);
        return title;
    }
}