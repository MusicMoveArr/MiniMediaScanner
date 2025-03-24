using System.Xml.Schema;
using ATL;
using FuzzySharp;
using MiniMediaScanner.Models;
using MiniMediaScanner.Models.MusicBrainz;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class GroupTaggingMBMetadataCommandHandler
{
    private readonly MusicBrainzAPIService _musicBrainzAPIService;
    private readonly MetadataRepository _metadataRepository;
    private readonly ArtistRepository _artistRepository;
    private readonly MediaTagWriteService _mediaTagWriteService;
    private readonly ImportCommandHandler _importCommandHandler;
    private readonly StringNormalizerService _normalizerService;
    private readonly MusicBrainzArtistRepository _musicBrainzArtistRepository;

    public GroupTaggingMBMetadataCommandHandler(string connectionString)
    {
        _musicBrainzAPIService = new MusicBrainzAPIService();
        _metadataRepository = new MetadataRepository(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
        _mediaTagWriteService = new MediaTagWriteService();
        _importCommandHandler = new ImportCommandHandler(connectionString);
        _normalizerService = new StringNormalizerService();
        _musicBrainzArtistRepository = new MusicBrainzArtistRepository(connectionString);
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
        MusicBrainzArtistModel? artistModel = await _musicBrainzArtistRepository.GetMusicBrainzDataByNameAsync(artist, album, string.Empty);

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
                        MatchedFor = Fuzz.Ratio(release.Country, artistCountry)
                    })
                    .OrderByDescending(match => match.MatchedFor)
                    .Select(match => match.Release)
                    .FirstOrDefault()
                : artistModel.Releases.OrderByDescending(r => r.Country).FirstOrDefault();
                //order by on purpose, most likely the first country it will grab is XW (World Wide), XE (Europe) etc instead of some random other not related country

        if (releaseCountry == null)
        {
            return;
        }
        
        List<MetadataModel> missingTracks = new List<MetadataModel>();
        MusicBrainzLabelInfoModel? labelInfo = await GetArtistLabelAsync(releaseCountry.Id);
        
        int updateSuccess = 0;
        foreach (MetadataModel metadata in metadataModels)
        {
            Track track = new Track(metadata.Path);

            MusicBrainzReleaseMediaTrackModel? foundTrack = releaseCountry
                ?.Media?.First()
                ?.Tracks
                ?.FirstOrDefault(t => Fuzz.Ratio(t.Title, track.Title) >= 90);

            if (foundTrack == null)
            {
                missingTracks.Add(metadata);

                MusicBrainzReleaseMediaTrackModel? matchTrack = null;
                if (!GetSecondBestTrackMatch(artistModel, track, out matchTrack))
                {
                    Console.WriteLine($"Could not find Track title '{track.Title}' of album '{album}' in our MusicBrainz database");
                    continue;
                }

                foundTrack = matchTrack;
            }

            try
            {
                await ProcessFileAsync(track, metadata, releaseCountry, artistModel, foundTrack, overwriteTagValue, labelInfo, confirm);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            updateSuccess++;
        }
    }

    private bool GetSecondBestTrackMatch(MusicBrainzArtistModel? artistModel, Track track,
                                         out MusicBrainzReleaseMediaTrackModel? matchTrack)
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
                        TrackTargetTitle = track.Title,
                        MatchedFor = Fuzz.Ratio(track.Title, t.Title)
                    })
                    .Where(t => t.MatchedFor >= 90)
                    .OrderByDescending(t => t.MatchedFor)
                    .FirstOrDefault()
            }).ToList();

        var bestMatch = foundTrack
            .OrderByDescending(match => match.Match?.MatchedFor)
            .ThenByDescending(match => match.Release.Country)
            .Select(match => match.Match?.Track)
            .FirstOrDefault();

        matchTrack = bestMatch;
        return bestMatch != null;
    }

    private async Task ProcessFileAsync(Track track
        , MetadataModel metadata
        , MusicBrainzArtistReleaseModel releaseCountry
        , MusicBrainzArtistModel artistModel
        , MusicBrainzReleaseMediaTrackModel mediaTrack
        , bool overwriteTagValue
        , MusicBrainzLabelInfoModel? labelInfo
        , bool autoConfirm)
    {
        bool trackInfoUpdated = false;
        string? musicBrainzTrackId = mediaTrack.Id;

        var bestMatchedArtist = artistModel?.ArtistCredit?.FirstOrDefault();
        
        string? musicBrainzReleaseArtistId = bestMatchedArtist?.Artist?.Id;
        string? musicBrainzAlbumId = releaseCountry.Id;
        string? musicBrainzReleaseGroupId = releaseCountry.ReleaseGroup.Id;
        
        string artists = string.Join(';', artistModel?.ArtistCredit.Select(artist => artist.Name));
        string musicBrainzArtistIds = string.Join(';', artistModel?.ArtistCredit.Select(artist => artist.Artist.Id));
        string isrcs = artistModel?.ISRCS != null ? string.Join(';', artistModel?.ISRCS) : string.Empty;

        if (labelInfo != null)
        {
            if (!string.IsNullOrWhiteSpace(labelInfo?.Label?.Name))
            {
                
                UpdateTag(track, "LABEL", labelInfo?.Label.Name, ref trackInfoUpdated, overwriteTagValue);
                UpdateTag(track, "CATALOGNUMBER", labelInfo?.CataLogNumber, ref trackInfoUpdated, overwriteTagValue);
            }
        }

        UpdateTag(track, "date", releaseCountry.Date, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "originaldate", releaseCountry.Date, ref trackInfoUpdated, overwriteTagValue);

        if (string.IsNullOrWhiteSpace(track.Title))
        {
            UpdateTag(track, "Title", mediaTrack.Title, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.Album))
        {
            UpdateTag(track, "Album", releaseCountry.Title, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.AlbumArtist)  || track.AlbumArtist.ToLower().Contains("various"))
        {
            UpdateTag(track, "AlbumArtist", bestMatchedArtist?.Name, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.Artist) || track.Artist.ToLower().Contains("various"))
        {
            UpdateTag(track, "Artist", bestMatchedArtist?.Name, ref trackInfoUpdated, overwriteTagValue);
        }

        //requires further testing
        //UpdateTag(track, "ARTISTS", artists, ref trackInfoUpdated, overwriteTagValue);
        
        UpdateTag(track, "ISRC", isrcs, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "SCRIPT", releaseCountry.TextRepresentation?.Script, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "barcode", releaseCountry.Barcode, ref trackInfoUpdated, overwriteTagValue);

        UpdateTag(track, "MusicBrainz Artist Id", musicBrainzArtistIds, ref trackInfoUpdated, overwriteTagValue);

        UpdateTag(track, "MusicBrainz Track Id", mediaTrack.Recording.Id, ref trackInfoUpdated, overwriteTagValue);
        
        UpdateTag(track, "MusicBrainz Release Track Id", musicBrainzTrackId, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "MusicBrainz Release Artist Id", musicBrainzReleaseArtistId, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "MusicBrainz Release Group Id", musicBrainzReleaseGroupId, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "MusicBrainz Release Id", releaseCountry.Id, ref trackInfoUpdated, overwriteTagValue);

        UpdateTag(track, "MusicBrainz Album Artist Id", musicBrainzArtistIds, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "MusicBrainz Album Id", musicBrainzAlbumId, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "MusicBrainz Album Type", releaseCountry.ReleaseGroup.PrimaryType?.ToLower(), ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "MusicBrainz Album Release Country", releaseCountry.Country, ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "MusicBrainz Album Status", releaseCountry.Status?.ToLower(), ref trackInfoUpdated, overwriteTagValue);
        
        if (releaseCountry.ReleaseGroup?.FirstReleaseDate?.Length >= 4)
        {
            UpdateTag(track, "originalyear", releaseCountry.ReleaseGroup.FirstReleaseDate.Substring(0, 4), ref trackInfoUpdated, overwriteTagValue);
        }

        var media = releaseCountry.Media?.FirstOrDefault();
        UpdateTag(track, "Disc Number", media?.Position?.ToString(), ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "Track Number", mediaTrack.Position?.ToString(), ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "Total Tracks", media?.Tracks?.Count.ToString(), ref trackInfoUpdated, overwriteTagValue);
        UpdateTag(track, "MEDIA", media?.Format, ref trackInfoUpdated, overwriteTagValue);

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

    private void UpdateTag(Track track, string tagName, string? value, ref bool trackInfoUpdated, bool overwriteTagValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (int.TryParse(value, out int intValue) && intValue == 0)
        {
            return;
        }
        
        tagName = _mediaTagWriteService.GetFieldName(track, tagName);
        value = _normalizerService.ReplaceInvalidCharacters(value);
        
        if (!overwriteTagValue &&
            (track.AdditionalFields.ContainsKey(tagName) ||
             !string.IsNullOrWhiteSpace(track.AdditionalFields[tagName])))
        {
            return;
        }
        
        string orgValue = string.Empty;
        bool tempIsUpdated = false;
        _mediaTagWriteService.UpdateTrackTag(track, tagName, value, ref tempIsUpdated, ref orgValue);

        if (tempIsUpdated)
        {
            Console.WriteLine($"Updating tag '{tagName}' value '{orgValue}' =>  '{value}'");
            trackInfoUpdated = true;
        }
    }
}