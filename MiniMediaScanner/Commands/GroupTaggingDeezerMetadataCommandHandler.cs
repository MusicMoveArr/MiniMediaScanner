using ATL;
using FuzzySharp;
using Microsoft.Extensions.Caching.Memory;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models;
using MiniMediaScanner.Models.Deezer;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class GroupTaggingDeezerMetadataCommandHandler
{
    private readonly MetadataRepository _metadataRepository;
    private readonly ArtistRepository _artistRepository;
    private readonly MediaTagWriteService _mediaTagWriteService;
    private readonly ImportCommandHandler _importCommandHandler;
    private readonly DeezerRepository _deezerRepository;
    private readonly MatchRepository _matchRepository;
    private readonly Dictionary<Guid, long> _metaArtistDeezer;
    private readonly FileMetaDataService _fileMetaDataService;
    private readonly AsyncLock _asyncLock;
    private readonly MemoryCache _cache;
    
    public GroupTaggingDeezerMetadataCommandHandler(string connectionString)
    {
        _metadataRepository = new MetadataRepository(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
        _mediaTagWriteService = new MediaTagWriteService();
        _importCommandHandler = new ImportCommandHandler(connectionString);
        _deezerRepository = new DeezerRepository(connectionString);
        _matchRepository = new MatchRepository(connectionString);
        _metaArtistDeezer = new Dictionary<Guid, long>();
        _fileMetaDataService = new FileMetaDataService();
        _asyncLock = new AsyncLock();
        var options = new MemoryCacheOptions();
        _cache = new MemoryCache(options);
    }
    
    public async Task TagMetadataAsync(string album, bool overwriteTagValue, bool confirm, bool overwriteAlbumTag)
    {
        foreach (var artist in await _artistRepository.GetAllArtistNamesAsync())
        {
            await TagMetadataAsync(artist, album, overwriteTagValue, confirm, overwriteAlbumTag);
        }
    }

    public async Task TagMetadataAsync(string artist, string album, bool overwriteTagValue, bool confirm, bool overwriteAlbumTag)
    {
        var metadata = (await _metadataRepository.GetMetadataByArtistAsync(artist))
            .Where(metadata => string.IsNullOrWhiteSpace(album) || 
                               string.Equals(metadata.AlbumName, album, StringComparison.OrdinalIgnoreCase))
            .Where(metadata => new FileInfo(metadata.Path).Exists)
            .ToList();

        Console.WriteLine($"Checking artist '{artist}', found {metadata.Count} tracks to process");

        foreach (var record in metadata.GroupBy(album => album.AlbumId))
        {
            try
            {
                await ProcessAlumGroupAsync(record.ToList(), artist, record.First().AlbumName!, overwriteTagValue,
                    confirm, overwriteAlbumTag);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
                
    private async Task ProcessAlumGroupAsync(List<MetadataModel> metadataModels
        , string artist
        , string album
        , bool overwriteTagValue
        , bool confirm
        , bool overwriteAlbumTag)
    {
        Guid? artistId = metadataModels.FirstOrDefault()?.ArtistId;
        long deezerArtistId = 0;

        using (await _asyncLock.LockAsync())
        {
            if (!_metaArtistDeezer.TryGetValue(artistId.Value, out deezerArtistId))
            {
                deezerArtistId = await _matchRepository.GetBestDeezerMatchAsync(artistId.Value, artist) ?? 0;
                _metaArtistDeezer.Add(artistId.Value, deezerArtistId);
            }
        }

        if (deezerArtistId == 0)
        {
            Console.WriteLine($"No match found for Deezer metadata, artist '{artist}'");
            return;
        }
        
        var deezerTracks = await _deezerRepository.GetTrackByArtistIdAsync(deezerArtistId, album, string.Empty);

        if (deezerTracks?.Count == 0)
        {
            Console.WriteLine($"For Artist '{artist}', Album '{album}' information not found in our Deezer database");
            return;
        }

        var allDeezerTracks = new List<DeezerTrackDbModel>();
        string deezerArtistTracksKey = $"DeezerArtistTracks_{deezerArtistId}";
        using (await _asyncLock.LockAsync())
        {
            if (!_cache.TryGetValue(deezerArtistTracksKey, out List<DeezerTrackDbModel>? result))
            {
                allDeezerTracks = await _deezerRepository.GetTrackByArtistIdAsync(deezerArtistId, string.Empty, string.Empty);
                MemoryCacheEntryOptions options = new()
                {
                    SlidingExpiration = TimeSpan.FromHours(1)
                };
                _cache.Set(deezerArtistTracksKey, allDeezerTracks, options);
            }
            else
            {
                allDeezerTracks = result;
            }
        }

        //match Deezer Tracks against our database
        //sort descending album's by the amount of matches
        //this method will prevent getting different AlbumId's for each track
        var matchedAlbums = deezerTracks
            .GroupBy(track => track.AlbumId)
            .Select(tracks => new
            {
                AlbumId = tracks.Key,
                Matches = tracks.Select(track => new
                {
                    Track = track,
                    MetadataTracks = metadataModels.Select(metadata => new
                    {
                        MetadataTrack = metadata,
                        DeezerTrack = track,
                        MatchedFor = Fuzz.Ratio(metadata.Title, track.TrackName)
                    })
                    .Where(match => match.MatchedFor >= 90)
                    .Where(match => FuzzyHelper.ExactNumberMatch(match.MetadataTrack.Title, match.DeezerTrack.TrackName))
                    .Where(match => FuzzyHelper.ExactNumberMatch(match.MetadataTrack.AlbumName, match.DeezerTrack.AlbumName))
                    .OrderByDescending(match => match.MatchedFor)
                    .ToList()
                        
                })
            })
            .OrderByDescending(tracks => tracks.Matches.Sum(matches => matches.MetadataTracks.Count))
            .ToList();
        
        List<Guid> processedMetadataIds = new List<Guid>();
        
        foreach(var matchedAlbum in matchedAlbums)
        {
            foreach (var matchedTrack in matchedAlbum.Matches)
            {
                foreach (var metadata in matchedTrack.MetadataTracks)
                {
                    if (processedMetadataIds.Contains(metadata.MetadataTrack.MetadataId!.Value))
                    {
                        continue;
                    }
                    
                    try
                    {
                        await ProcessFileAsync(metadata.MetadataTrack, metadata.DeezerTrack, overwriteTagValue, 
                                               confirm, overwriteAlbumTag, deezerArtistId);
                        processedMetadataIds.Add(metadata.MetadataTrack.MetadataId!.Value);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
        }
        
        var notProcessed = metadataModels
            .Where(metadata => !processedMetadataIds.Contains(metadata.MetadataId!.Value))
            .ToList();

        foreach (var metadata in notProcessed)
        {
            //handle incorrectly tagged albums
            var foundTrack = GetSecondBestTrackMatch(allDeezerTracks, metadata.Title, metadata.AlbumName);
            if (foundTrack == null)
            {
                Console.WriteLine($"Could not find Track title '{metadata.Title}' of album '{album}' in our Deezer database");
                continue;
            }
            
            try
            {
                await ProcessFileAsync(metadata, foundTrack, overwriteTagValue, confirm, overwriteAlbumTag, deezerArtistId);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
    
    private DeezerTrackDbModel? GetSecondBestTrackMatch(List<DeezerTrackDbModel> deezerTracks, 
        string trackTitle,
        string albumTitle)
    {
        var foundTracks = deezerTracks
            .Select(deezerTrack => new
            {
                DeezerTrack = deezerTrack,
                TrackMatchedFor = Fuzz.Ratio(trackTitle, deezerTrack.TrackName),
                AlbumMatchedFor = Fuzz.Ratio(albumTitle, deezerTrack.AlbumName)
            })
            .Where(match => match.TrackMatchedFor >= 80)
            .Where(match => match.AlbumMatchedFor >= 80)
            .Where(match => FuzzyHelper.ExactNumberMatch(trackTitle, match.DeezerTrack.TrackName))
            .Where(match => FuzzyHelper.ExactNumberMatch(albumTitle, match.DeezerTrack.AlbumName))
            .OrderByDescending(match => match.TrackMatchedFor)
            .ThenByDescending(match => match.AlbumMatchedFor)
            .Select(match => match.DeezerTrack)
            .ToList();

        return foundTracks.FirstOrDefault();
    }

    private async Task ProcessFileAsync(
        MetadataModel metadata
        , DeezerTrackDbModel deezerTrack
        , bool overwriteTagValue
        , bool autoConfirm
        , bool overwriteAlbumTag
        , long deezerArtistId)
    {
        Track track = new Track(metadata.Path);
        Console.WriteLine($"Processing file: {metadata.Path}");
        var metadataInfo = _fileMetaDataService.GetMetadataInfo(track);
        
        bool trackInfoUpdated = false;
        var trackArtists = await _deezerRepository.GetTrackArtistsAsync(deezerTrack.TrackId, deezerArtistId);
        string artists = string.Join(';', trackArtists);

        if (string.IsNullOrWhiteSpace(track.Title) || overwriteTagValue)
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "Title", deezerTrack.TrackName, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.Album) || overwriteAlbumTag)
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "Album", deezerTrack.AlbumName, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.AlbumArtist)  || track.AlbumArtist.ToLower().Contains("various"))
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "AlbumArtist", deezerTrack.ArtistName, ref trackInfoUpdated, overwriteTagValue);
        }
        if (string.IsNullOrWhiteSpace(track.Artist) || track.Artist.ToLower().Contains("various"))
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "Artist",  deezerTrack.ArtistName, ref trackInfoUpdated, overwriteTagValue);
        }
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Track Id", deezerTrack.TrackId.ToString(), ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Track Explicit", deezerTrack.ExplicitLyrics ? "Y": "N", ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Track Href", deezerTrack.TrackHref, ref trackInfoUpdated, overwriteTagValue);
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Album Id", deezerTrack.AlbumId.ToString(), ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Album Href", deezerTrack.AlbumHref, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Album Release Date", deezerTrack.AlbumReleaseDate, ref trackInfoUpdated, overwriteTagValue);
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Artist Id", deezerTrack.ArtistId.ToString(), ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Artist Href", deezerTrack.ArtistHref, ref trackInfoUpdated, overwriteTagValue);
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "ARTISTS", artists, ref trackInfoUpdated, overwriteTagValue);

        _mediaTagWriteService.UpdateTag(track, metadataInfo, "ISRC", deezerTrack.TrackISRC, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "UPC", deezerTrack.AlbumUPC, ref trackInfoUpdated, overwriteTagValue);
            
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Date", deezerTrack.AlbumReleaseDate, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Label", deezerTrack.Label, ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Disc Number", deezerTrack.DiscNumber.ToString(), ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Track Number", deezerTrack.TrackPosition.ToString(), ref trackInfoUpdated, overwriteTagValue);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Total Tracks", deezerTrack.AlbumTotalTracks.ToString(), ref trackInfoUpdated, overwriteTagValue);

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
}