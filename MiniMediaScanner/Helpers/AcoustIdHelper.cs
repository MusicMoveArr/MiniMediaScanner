using MiniMediaScanner.Models.AcoustId;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Helpers;

public class AcoustIdHelper
{
    public static async Task<AcoustIdRecording?> GetBestMatchingAcoustIdAsync(
        MusicBrainzAPIService musicBrainzApiService,
        AcoustIdResponse? acoustIdResponse, 
        string artist,
        string album,
        string title,
        int trackDuration,
        int matchPercentageTags,
        int matchPercentageAcoustId)
    {
        if (acoustIdResponse?.Results?.Count == 0)
        {
            return null;
        }

        var highestScoreResult = acoustIdResponse
            ?.Results
            ?.Where(result => result.Recordings?.Any() == true)
            .Where(result => result.Score >= (matchPercentageAcoustId / 100F))
            .OrderByDescending(result => result.Score)
            .FirstOrDefault();

        if (highestScoreResult == null)
        {
            return null;
        }

        //perhaps not the best approach but sometimes...
        bool ignoreFilters = string.IsNullOrWhiteSpace(album) ||
                             string.IsNullOrWhiteSpace(artist) ||
                             string.IsNullOrWhiteSpace(title);

        var recordingReleases = highestScoreResult.Recordings
            .Where(x => GuidHelper.GuidHasValue(x.Id))
            .Select(async x => new
            {
                RecordingId = x.Id,
                Recording = await musicBrainzApiService.GetRecordingByIdAsync(x.Id.Value)
            })
            .Select(x => x.Result)
            .ToList();
        
        var results = highestScoreResult
            .Recordings
           ?.Select(result => new
           {
               Result = result,
               Releases = recordingReleases.FirstOrDefault(release => string.Equals(release.RecordingId, result.Id))
           })
            ?.Select(result => new
            {
                AlbumMatchedFor = result.Releases.Recording.Releases
                    .Where(release => ignoreFilters || FuzzyHelper.ExactNumberMatch(release.Title, album))
                    .Select(release => new
                    {
                        MatchedFor = FuzzyHelper.FuzzTokenSortRatioToLower(release.Title, album),
                        Release = release
                    })
                    .OrderByDescending(match => match.MatchedFor)
                    .FirstOrDefault(),
                ArtistMatchedFor = result.Result.Artists?.Sum(a => FuzzyHelper.FuzzTokenSortRatioToLower(a.Name, artist)) ?? 0,
                TitleMatchedFor = FuzzyHelper.FuzzTokenSortRatioToLower(title, result.Result.Title),
                LengthMatch = Math.Abs(trackDuration - result.Result.Duration ?? 100),
                AcoustIdResult = result
            })
            .Where(match => ignoreFilters || FuzzyHelper.ExactNumberMatch(title, match.AcoustIdResult.Result.Title))
            .Where(match => ignoreFilters || match.ArtistMatchedFor >= matchPercentageTags)
            .Where(match => ignoreFilters || match.TitleMatchedFor >= matchPercentageTags)
            .OrderByDescending(result => result.ArtistMatchedFor)
            .ThenByDescending(result => result.AlbumMatchedFor?.MatchedFor)
            .ThenByDescending(result => result.TitleMatchedFor)
            .ThenBy(result => result.LengthMatch)
            .Select(result => result)
            .ToList();

        var bestResult = results.FirstOrDefault();
        AcoustIdRecording? firstResult = bestResult?.AcoustIdResult.Result;
        if (firstResult != null)
        {
            firstResult.RecordingRelease = bestResult.AlbumMatchedFor?.Release;
            firstResult.AcoustId = highestScoreResult.Id;
        }
        return firstResult;
    }
}