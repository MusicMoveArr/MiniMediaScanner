using System.Xml.Schema;
using ATL;
using FuzzySharp;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models;
using MiniMediaScanner.Models.MusicBrainz;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class GroupTaggingMBMetadataCommandHandler
{
    private const int MustMatchFor = 80;
    private readonly MusicBrainzAPIService _musicBrainzAPIService;
    private readonly MetadataRepository _metadataRepository;
    private readonly ArtistRepository _artistRepository;
    private readonly MediaTagWriteService _mediaTagWriteService;
    private readonly ImportCommandHandler _importCommandHandler;
    private readonly MusicBrainzArtistRepository _musicBrainzArtistRepository;
    private readonly FileMetaDataService _fileMetaDataService;

    public GroupTaggingMBMetadataCommandHandler(string connectionString)
    {
        _musicBrainzAPIService = new MusicBrainzAPIService();
        _metadataRepository = new MetadataRepository(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
        _mediaTagWriteService = new MediaTagWriteService();
        _importCommandHandler = new ImportCommandHandler(connectionString);
        _musicBrainzArtistRepository = new MusicBrainzArtistRepository(connectionString);
        _fileMetaDataService = new FileMetaDataService();
    }
    
    public async Task TagMetadataAsync(string album, bool overwriteTagValue, bool confirm)
    {
        foreach (var artist in await _artistRepository.GetAllArtistNamesAsync())
        {
            await TagMetadataAsync(artist, album, overwriteTagValue, confirm);
        }
    }

    public async Task TagMetadataAsync(string artist, string album, bool overwriteTagValue, bool confirm)
    {
        var metadata = (await _metadataRepository.GetMetadataByArtistAsync(artist))
            .Where(metadata => string.IsNullOrWhiteSpace(album) || 
                               string.Equals(metadata.AlbumName, album, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Console.WriteLine($"Checking artist '{artist}', found {metadata.Count} tracks to process");

        foreach (var record in metadata.GroupBy(album => album.AlbumId))
        {
            try
            {
                await ProcessAlumGroupAsync(record.ToList(), artist, record.First().AlbumName!, overwriteTagValue, confirm);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
                
    private async Task ProcessAlumGroupAsync(List<MetadataModel> metadataModels, string artist, string album, bool overwriteTagValue, bool confirm)
    {
        MusicBrainzArtistModel? artistModel = (await _musicBrainzArtistRepository.GetMusicBrainzDataByNameAsync(artist, album, string.Empty))
            .FirstOrDefault();
        
        if (artistModel == null)
        {
            Console.WriteLine($"For Artist '{artist}', Album '{album}' information not found in our MusicBrainz database");
            return;
        }

        string? artistCountry = artistModel.ArtistCredit.FirstOrDefault()?.Artist?.Country;

        var releaseCountry =
            !string.IsNullOrWhiteSpace(artistCountry)
                ? artistModel.Releases
                    .Select(release => new
                    {
                        Release = release,
                        CountryMatchedFor = FuzzyHelper.FuzzRatioToLower(release.Country, artistCountry),
                        AlbumMatchedFor = FuzzyHelper.FuzzRatioToLower(release.Title, album)
                    })
                    .OrderByDescending(match => match.CountryMatchedFor)
                    .ThenByDescending(match => match.AlbumMatchedFor)
                    .Where(t => FuzzyHelper.ExactNumberMatch(t.Release.Title, album))
                    .Where(match => match.AlbumMatchedFor >= MustMatchFor)
                    .Select(match => match.Release)
                    .FirstOrDefault()
                : artistModel.Releases.OrderByDescending(r => r.Country).FirstOrDefault();
                //order by on purpose, most likely the first country it will grab is XW (World Wide), XE (Europe) etc instead of some random other not related country

        if (releaseCountry == null)
        {
            return;
        }
        
        List<MetadataModel> missingTracks = new List<MetadataModel>();
        
        int updateSuccess = 0;
        foreach (MetadataModel metadata in metadataModels)
        {
            Track track = new Track(metadata.Path);

            var foundTracks = releaseCountry
                ?.Media?.First()
                ?.Tracks
                .Select(t => new
                {
                    MatchedFor = FuzzyHelper.FuzzRatioToLower(t.Title, track.Title),
                    Track = t
                })
                ?.Where(t => t.MatchedFor >= MustMatchFor)
                ?.Where(t => FuzzyHelper.ExactNumberMatch(t.Track.Title, track.Title))
                .OrderByDescending(t => t.MatchedFor)
                .Select(t => t.Track)
                .ToList();

            MusicBrainzReleaseMediaTrackModel? foundTrack = foundTracks?.FirstOrDefault();
            MusicBrainzArtistReleaseModel? matchRelease = releaseCountry;

            if (foundTrack == null)
            {
                missingTracks.Add(metadata);

                MusicBrainzReleaseMediaTrackModel? matchTrack = null;
                if (!GetSecondBestTrackMatch(artistModel, track, out matchTrack, out matchRelease))
                {
                    Console.WriteLine($"Could not find Track title '{track.Title}' of album '{album}' in our MusicBrainz database");
                    continue;
                }

                foundTrack = matchTrack;
            }

            try
            {
                await ProcessFileAsync(track, metadata, matchRelease, artistModel, foundTrack, overwriteTagValue, confirm);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            updateSuccess++;
        }
    }

    private bool GetSecondBestTrackMatch(MusicBrainzArtistModel? artistModel, Track track,
                                         out MusicBrainzReleaseMediaTrackModel? matchTrack,
                                         out MusicBrainzArtistReleaseModel? matchRelease)
    {
        matchTrack = null;
        
        var foundTrack = artistModel?.Releases
            .Select(release => new
            {
                Release = release,
                Match = release?.Media?.First()
                    ?.Tracks
                    ?.Select(t => new
                    {
                        Track = t,
                        TrackTitle = t.Title,
                        TrackMatchedFor = FuzzyHelper.FuzzRatioToLower(track.Title, t.Title),
                        AlbumMatchedFor = FuzzyHelper.FuzzRatioToLower(track.Album, release.Title)
                    })
                    .Where(t => t.TrackMatchedFor >= MustMatchFor)
                    .Where(t => t.AlbumMatchedFor >= MustMatchFor)
                    .Where(match => FuzzyHelper.ExactNumberMatch(track.Title, match.TrackTitle))
                    .Where(match => FuzzyHelper.ExactNumberMatch(track.Album, release.Title))
                    .OrderByDescending(t => t.TrackMatchedFor)
                    .FirstOrDefault()
            }).ToList();

        var bestMatch = foundTrack
            ?.Where(match => match?.Match?.Track != null)
            .OrderByDescending(match => match.Match?.TrackMatchedFor)
            .ThenByDescending(match => match.Match?.AlbumMatchedFor)
            .ThenByDescending(match => match.Release.Country)
            .Select(match => match)
            .FirstOrDefault();

        matchTrack = bestMatch?.Match?.Track;
        matchRelease = bestMatch?.Release;
        return bestMatch != null;
    }

    private async Task ProcessFileAsync(Track track
        , MetadataModel metadata
        , MusicBrainzArtistReleaseModel releaseCountry
        , MusicBrainzArtistModel artistModel
        , MusicBrainzReleaseMediaTrackModel mediaTrack
        , bool overwriteTagValue
        , bool autoConfirm)
    {
        var metadataInfo = await _fileMetaDataService.GetMetadataInfoAsync(new FileInfo(track.Path));
        
        bool trackInfoUpdated = false;
        string? musicBrainzTrackId = mediaTrack.Id;

        var bestMatchedArtist = artistModel?.ArtistCredit?.FirstOrDefault();
        
        string? musicBrainzReleaseArtistId = bestMatchedArtist?.Artist?.Id;
        string? musicBrainzAlbumId = releaseCountry.Id;
        string? musicBrainzReleaseGroupId = releaseCountry.ReleaseGroup.Id;
        
        string artists = string.Join(';', mediaTrack.Recording.ArtistCredit?.Select(artist => artist.Name));
        string musicBrainzArtistIds = string.Join(';', mediaTrack.Recording.ArtistCredit.Select(artist => artist.Artist.Id));
        string isrcs = artistModel?.ISRCS != null ? string.Join(';', artistModel?.ISRCS) : string.Empty;

        if (releaseCountry.LabeLInfo?.Count > 0)
        {
            var labelInfo = releaseCountry.LabeLInfo.First();
            if (!string.IsNullOrWhiteSpace(labelInfo?.Label?.Name))
            {
                _mediaTagWriteService.UpdateTag(track, metadataInfo, "LABEL", labelInfo?.Label.Name, ref trackInfoUpdated, overwriteTagValue);
                _mediaTagWriteService.UpdateTag(track, metadataInfo, "CATALOGNUMBER", labelInfo?.CataLogNumber, ref trackInfoUpdated, overwriteTagValue);
            }
        }

        _mediaTagWriteService.UpdateTag(track, metadataInfo, "date", releaseCountry.Date, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "originaldate", releaseCountry.Date, ref trackInfoUpdated, overwriteTagValue);

        if (string.IsNullOrWhiteSpace(track.Title))
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "Title", mediaTrack.Title, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.Album))
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "Album", releaseCountry.Title, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.AlbumArtist)  || track.AlbumArtist.ToLower().Contains("various"))
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "AlbumArtist", bestMatchedArtist?.Name, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.Artist) || track.Artist.ToLower().Contains("various"))
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "Artist", bestMatchedArtist?.Name, ref trackInfoUpdated, overwriteTagValue);
        }

        //requires further testing
        //UpdateTag(track, "ARTISTS", artists, ref trackInfoUpdated, overwriteTagValue);
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "ISRC", isrcs, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "SCRIPT", releaseCountry.TextRepresentation?.Script, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "barcode", releaseCountry.Barcode, ref trackInfoUpdated, overwriteTagValue);

        _mediaTagWriteService.UpdateTag(track, metadataInfo, "MusicBrainz Artist Id", musicBrainzArtistIds, ref trackInfoUpdated, overwriteTagValue);

        _mediaTagWriteService.UpdateTag(track, metadataInfo, "MusicBrainz Track Id", mediaTrack.Recording.Id, ref trackInfoUpdated, overwriteTagValue);
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "MusicBrainz Release Track Id", musicBrainzTrackId, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "MusicBrainz Release Artist Id", musicBrainzReleaseArtistId, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "MusicBrainz Release Group Id", musicBrainzReleaseGroupId, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "MusicBrainz Release Id", releaseCountry.Id, ref trackInfoUpdated, overwriteTagValue);

        _mediaTagWriteService.UpdateTag(track, metadataInfo, "MusicBrainz Album Artist Id", musicBrainzArtistIds, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "MusicBrainz Album Id", musicBrainzAlbumId, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "MusicBrainz Album Type", releaseCountry.ReleaseGroup.PrimaryType?.ToLower(), ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "MusicBrainz Album Release Country", releaseCountry.Country, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "MusicBrainz Album Status", releaseCountry.Status?.ToLower(), ref trackInfoUpdated, overwriteTagValue);
        
        if (releaseCountry.ReleaseGroup?.FirstReleaseDate?.Length >= 4)
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "originalyear", releaseCountry.ReleaseGroup.FirstReleaseDate.Substring(0, 4), ref trackInfoUpdated, overwriteTagValue);
        }

        var media = releaseCountry.Media?.FirstOrDefault();
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Disc Number", media?.Position?.ToString(), ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Track Number", mediaTrack.Position?.ToString(), ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Total Tracks", media?.Tracks?.Count.ToString(), ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "MEDIA", media?.Format, ref trackInfoUpdated, overwriteTagValue);

        if (!trackInfoUpdated)
        {
            return;
        }

        Console.WriteLine("Confirm changes? (Y/y or N/n)");
        bool confirm = autoConfirm || Console.ReadLine()?.ToLower() == "y";
        
        if (confirm && trackInfoUpdated && await _mediaTagWriteService.SafeSaveAsync(track))
        {
            await _importCommandHandler.ProcessFileAsync(metadata.Path);
        }
    }

    private async Task<MusicBrainzLabelInfoModel?> GetArtistLabelAsync(string releaseId)
    {
        if (Guid.TryParse(releaseId, out var releaseGuid))
        {
            return (await _musicBrainzAPIService.GetReleaseWithLabelAsync(releaseGuid))
                ?.LabeLInfo.FirstOrDefault(label => label?.Label?.Type?.ToLower().Contains("production") == true);
        }
        return null;
    }
}