using ATL;
using FuzzySharp;
using Microsoft.Extensions.Caching.Memory;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models;
using MiniMediaScanner.Models.Tidal;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class GroupTaggingTidalMetadataCommandHandler
{
    private readonly MetadataRepository _metadataRepository;
    private readonly ArtistRepository _artistRepository;
    private readonly MediaTagWriteService _mediaTagWriteService;
    private readonly ImportCommandHandler _importCommandHandler;
    private readonly TidalRepository _tidalRepository;
    private readonly MatchRepository _matchRepository;
    private readonly Dictionary<Guid, int> _metaArtistTidal;
    private readonly FileMetaDataService _fileMetaDataService;
    private readonly AsyncLock _asyncLock;
    private readonly MemoryCache _cache;
    
    public bool Confirm { get; set; }
    public bool OverwriteTag { get; set; }
    public bool OverwriteArtist { get; set; }
    public bool OverwriteAlbumArtist { get; set; }
    public bool OverwriteAlbum { get; set; }
    public bool OverwriteTrack { get; set; }
    
    
    public GroupTaggingTidalMetadataCommandHandler(string connectionString)
    {
        _metadataRepository = new MetadataRepository(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
        _mediaTagWriteService = new MediaTagWriteService();
        _importCommandHandler = new ImportCommandHandler(connectionString);
        _tidalRepository = new TidalRepository(connectionString);
        _matchRepository = new MatchRepository(connectionString);
        _metaArtistTidal = new Dictionary<Guid, int>();
        _fileMetaDataService = new FileMetaDataService();
        _asyncLock = new AsyncLock();
        var options = new MemoryCacheOptions();
        _cache = new MemoryCache(options);
    }
    
    public async Task TagMetadataAsync(string album)
    {
        foreach (var artist in await _artistRepository.GetAllArtistNamesAsync())
        {
            await TagMetadataAsync(artist, album);
        }
    }

    public async Task TagMetadataAsync(string artist, string album)
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
                await ProcessAlumGroupAsync(
                    record.ToList(), 
                    artist, 
                    record.First().AlbumName!);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
                
    private async Task ProcessAlumGroupAsync(
        List<MetadataModel> metadataModels,
        string artist,
        string album)
    {
        Guid? artistId = metadataModels.FirstOrDefault()?.ArtistId;
        int tidalArtistId = 0;

        using (await _asyncLock.LockAsync())
        {
            if (!_metaArtistTidal.TryGetValue(artistId.Value, out tidalArtistId))
            {
                tidalArtistId = await _matchRepository.GetBestTidalMatchAsync(artistId.Value, artist) ?? 0;
                _metaArtistTidal.Add(artistId.Value, tidalArtistId);
            }
        }

        if (tidalArtistId == 0)
        {
            Console.WriteLine($"No match found for Tidal metadata, artist '{artist}'");
            return;
        }
        
        var tidalTracks = await _tidalRepository.GetTrackByArtistIdAsync(tidalArtistId, album, string.Empty);

        if (tidalTracks?.Count == 0)
        {
            Console.WriteLine($"For Artist '{artist}', Album '{album}' information not found in our Tidal database");
            return;
        }

        var allTidalTracks = new List<TidalTrackModel>();
        string tidalArtistTracksKey = $"TidalArtistTracks_{tidalArtistId}";
        using (await _asyncLock.LockAsync())
        {
            if (!_cache.TryGetValue(tidalArtistTracksKey, out List<TidalTrackModel>? result))
            {
                allTidalTracks = await _tidalRepository.GetTrackByArtistIdAsync(tidalArtistId, string.Empty, string.Empty);
                MemoryCacheEntryOptions options = new()
                {
                    SlidingExpiration = TimeSpan.FromHours(1)
                };
                _cache.Set(tidalArtistTracksKey, allTidalTracks, options);
            }
            else
            {
                allTidalTracks = result;
            }
        }

        //match Tidal Tracks against our database
        //sort descending album's by the amount of matches
        //this method will prevent getting different AlbumId's for each track
        var matchedAlbums = tidalTracks
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
                        TidalTrack = track,
                        MatchedFor = Fuzz.Ratio(metadata.Title, track.FullTrackName)
                    })
                    .Where(match => match.MatchedFor >= 90)
                    .Where(match => FuzzyHelper.ExactNumberMatch(match.MetadataTrack.Title, match.TidalTrack.FullTrackName))
                    .Where(match => FuzzyHelper.ExactNumberMatch(match.MetadataTrack.AlbumName, match.TidalTrack.AlbumName))
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
                        await ProcessFileAsync(metadata.MetadataTrack, metadata.TidalTrack, tidalArtistId);
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
            var foundTrack = GetSecondBestTrackMatch(allTidalTracks, metadata.Title, metadata.AlbumName);
            if (foundTrack == null)
            {
                Console.WriteLine($"Could not find Track title '{metadata.Title}' of album '{album}' in our Tidal database");
                continue;
            }
            
            try
            {
                await ProcessFileAsync(metadata, foundTrack, tidalArtistId);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
    
    private TidalTrackModel? GetSecondBestTrackMatch(
        List<TidalTrackModel> tidalTracks, 
        string trackTitle,
        string albumTitle)
    {
        var foundTracks = tidalTracks
            .Select(tidalTrack => new
            {
                TidalTrack = tidalTrack,
                TrackMatchedFor = Fuzz.Ratio(trackTitle, tidalTrack.FullTrackName),
                AlbumMatchedFor = Fuzz.Ratio(albumTitle, tidalTrack.AlbumName)
            })
            .Where(match => match.TrackMatchedFor >= 80)
            .Where(match => match.AlbumMatchedFor >= 80)
            .Where(match => FuzzyHelper.ExactNumberMatch(trackTitle, match.TidalTrack.FullTrackName))
            .Where(match => FuzzyHelper.ExactNumberMatch(albumTitle, match.TidalTrack.AlbumName))
            .OrderByDescending(match => match.TrackMatchedFor)
            .ThenByDescending(match => match.AlbumMatchedFor)
            .Select(match => match.TidalTrack)
            .ToList();

        return foundTracks.FirstOrDefault();
    }

    private async Task ProcessFileAsync(
        MetadataModel metadata
        , TidalTrackModel tidalTrack
        , int tidalArtistId)
    {
        Track track = new Track(metadata.Path);
        var metadataInfo = _fileMetaDataService.GetMetadataInfo(track);
        
        bool trackInfoUpdated = false;
        var trackArtists = await _tidalRepository.GetTrackArtistsAsync(tidalTrack.TrackId, tidalArtistId);
        string artists = string.Join(';', trackArtists);

        if (string.IsNullOrWhiteSpace(track.Title) || OverwriteTrack)
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "Title", tidalTrack.FullTrackName, ref trackInfoUpdated, OverwriteTag);
        }
        if (string.IsNullOrWhiteSpace(track.Album) || OverwriteAlbum)
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "Album", tidalTrack.AlbumName, ref trackInfoUpdated, OverwriteTag);
        }
        if (string.IsNullOrWhiteSpace(track.AlbumArtist)  || OverwriteAlbumArtist)
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "AlbumArtist", tidalTrack.ArtistName, ref trackInfoUpdated, OverwriteTag);
        }
        if (string.IsNullOrWhiteSpace(track.Artist) || OverwriteArtist)
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "Artist",  tidalTrack.ArtistName, ref trackInfoUpdated, OverwriteTag);
        }
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Tidal Track Id", tidalTrack.TrackId.ToString(), ref trackInfoUpdated, OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Tidal Track Explicit", tidalTrack.Explicit ? "Y": "N", ref trackInfoUpdated, OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Tidal Track Href", tidalTrack.TrackHref, ref trackInfoUpdated, OverwriteTag);
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Tidal Album Id", tidalTrack.AlbumId.ToString(), ref trackInfoUpdated, OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Tidal Album Href", tidalTrack.AlbumHref, ref trackInfoUpdated, OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Tidal Album Release Date", tidalTrack.ReleaseDate, ref trackInfoUpdated, OverwriteTag);
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Tidal Artist Id", tidalTrack.ArtistId.ToString(), ref trackInfoUpdated, OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Tidal Artist Href", tidalTrack.ArtistHref, ref trackInfoUpdated, OverwriteTag);
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "ARTISTS", artists, ref trackInfoUpdated, OverwriteTag);

        _mediaTagWriteService.UpdateTag(track, metadataInfo, "ISRC", tidalTrack.TrackISRC, ref trackInfoUpdated, OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "UPC", tidalTrack.AlbumUPC, ref trackInfoUpdated, OverwriteTag);
            
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Date", tidalTrack.ReleaseDate, ref trackInfoUpdated, OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Copyright", tidalTrack.Copyright, ref trackInfoUpdated, OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Disc Number", tidalTrack.DiscNumber.ToString(), ref trackInfoUpdated, OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Track Number", tidalTrack.TrackNumber.ToString(), ref trackInfoUpdated, OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Total Tracks", tidalTrack.TotalTracks.ToString(), ref trackInfoUpdated, OverwriteTag);

        if (!trackInfoUpdated)
        {
            return;
        }

        Console.WriteLine("Confirm changes? (Y/y or N/n)");
        bool confirm = this.Confirm || Console.ReadLine()?.ToLower() == "y";
        
        if (confirm && trackInfoUpdated && await _mediaTagWriteService.SafeSaveAsync(track))
        {
            await _importCommandHandler.ProcessFileAsync(metadata.Path);
        }
    }
}