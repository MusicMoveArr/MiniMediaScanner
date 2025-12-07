using ATL;
using Microsoft.Extensions.Caching.Memory;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models;
using MiniMediaScanner.Models.Tidal;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class TagMissingTidalMetadataCommandHandler
{
    private readonly MetadataRepository _metadataRepository;
    private readonly ArtistRepository _artistRepository;
    private readonly MediaTagWriteService _mediaTagWriteService;
    private readonly ImportCommandHandler _importCommandHandler;
    private readonly MatchRepository _matchRepository;
    private readonly TidalRepository _tidalRepository;
    private readonly FileMetaDataService _fileMetaDataService;
    private readonly AsyncLock _asyncLock;
    private readonly MemoryCache _cache;
    private readonly TrackScoreService _trackScoreService;
    
    public bool Confirm { get; set; }
    public bool OverwriteTag { get; set; }
    public bool OverwriteArtist { get; set; }
    public bool OverwriteAlbumArtist { get; set; }
    public bool OverwriteAlbum { get; set; }
    public bool OverwriteTrack { get; set; }

    public TagMissingTidalMetadataCommandHandler(string connectionString)
    {
        _metadataRepository = new MetadataRepository(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
        _mediaTagWriteService = new MediaTagWriteService();
        _importCommandHandler = new ImportCommandHandler(connectionString);
        _matchRepository = new MatchRepository(connectionString);
        _tidalRepository = new TidalRepository(connectionString);
        _fileMetaDataService = new FileMetaDataService();
        _asyncLock = new AsyncLock();
        var options = new MemoryCacheOptions();
        _cache = new MemoryCache(options);
        _trackScoreService = new TrackScoreService();
    }
    
    public async Task TagMetadataAsync()
    {
        await ParallelHelper.ForEachAsync(await _artistRepository.GetAllArtistNamesAsync(), 4, async artist =>
        {
            try
            {
                await TagMetadataAsync(artist, string.Empty);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        });
    }

    public async Task TagMetadataAsync(string artist, string album)
    {
        var metadata = (await _metadataRepository.GetMetadataByArtistAsync(artist))
            .Where(metadata => string.IsNullOrWhiteSpace(album) || 
                               string.Equals(metadata.AlbumName, album, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Console.WriteLine($"Checking artist '{artist}', found {metadata.Count} tracks to process");

        foreach (var record in metadata.Where(r => new FileInfo(r.Path).Exists))
        {
            try
            {
                await ProcessFileAsync(record);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
                
    private async Task ProcessFileAsync(MetadataModel metadata)
    {
        int? tidalArtistId = await _matchRepository.GetBestTidalMatchAsync(metadata.ArtistId.Value, metadata.ArtistName);
        
        if (!tidalArtistId.HasValue)
        {
            Console.WriteLine($"Nothing found for '{metadata.AlbumName}', '{metadata.Title}'");
            return;
        }
        
        var allTidalTracks = new List<TrackScoreComparerTidalModel>();
        string tidalArtistTracksKey = $"TidalArtistTracks_{tidalArtistId}";
        using (await _asyncLock.LockAsync())
        {
            if (!_cache.TryGetValue(tidalArtistTracksKey, out List<TrackScoreComparerTidalModel>? result))
            {
                var tempTidalTracks = await _tidalRepository.GetTrackByArtistIdAsync(tidalArtistId.Value, string.Empty, string.Empty);
                allTidalTracks.AddRange(tempTidalTracks
                    .Select(model => new TrackScoreComparerTidalModel(model)));
                
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

        var trackScoreTargetModel = new TrackScoreTargetModel
        {
            MetadataId = metadata.MetadataId!.Value,
            Artist = metadata.ArtistName ?? string.Empty,
            Album = metadata.AlbumName ?? string.Empty,
            AlbumId = metadata.AlbumId?.ToString() ?? string.Empty,
            Date = metadata.Tag_Date ?? string.Empty,
            Isrc = metadata.Tag_Isrc ?? string.Empty,
            Title = metadata.Title ?? string.Empty,
            Duration = metadata.TrackLength,
            TrackNumber = metadata.Tag_Track,
            TrackTotalCount = metadata.Tag_TrackCount,
            Upc = metadata.Tag_Upc ?? string.Empty
        };

        var tidalScoreResult = _trackScoreService
            .GetFirstTrackScore(trackScoreTargetModel, allTidalTracks, 80);
        
        if (tidalScoreResult == null)
        {
            Console.WriteLine($"Nothing found for '{metadata.AlbumName}', '{metadata.Title}'");
            return;
        }
        
        TidalTrackModel tidalTrack = ((TrackScoreComparerTidalModel)tidalScoreResult.TrackScoreComparer).TrackModel;
        var trackArtists = await _tidalRepository.GetTrackArtistsAsync(tidalTrack.TrackId, tidalArtistId.Value);
        string artists = string.Join(';', trackArtists);
        
        Console.WriteLine($"Release found for '{metadata.Path}'");

        Track track = new Track(metadata.Path);
        var metadataInfo = await _fileMetaDataService.GetMetadataInfoAsync(new FileInfo(track.Path));
        bool trackInfoUpdated = false;
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Tidal Track Id", tidalTrack.TrackId.ToString(), ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Tidal Track Explicit", tidalTrack.Explicit ? "Y": "N", ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Tidal Track Href", tidalTrack.TrackHref, ref trackInfoUpdated, this.OverwriteTag);
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Tidal Album Id", tidalTrack.AlbumId.ToString(), ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Tidal Album Href", tidalTrack.AlbumHref, ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Tidal Album Release Date", tidalTrack.ReleaseDate, ref trackInfoUpdated, this.OverwriteTag);
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Tidal Artist Id", tidalTrack.ArtistId.ToString(), ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Tidal Artist Href", tidalTrack.ArtistHref, ref trackInfoUpdated, this.OverwriteTag);
        
        if (string.IsNullOrWhiteSpace(track.Title) || this.OverwriteTrack)
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "Title", tidalTrack.FullTrackName, ref trackInfoUpdated, this.OverwriteTag);
        }
        if (string.IsNullOrWhiteSpace(track.Album) || this.OverwriteAlbum)
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "Album", tidalTrack.AlbumName, ref trackInfoUpdated, this.OverwriteTag);
        }
        if (string.IsNullOrWhiteSpace(track.AlbumArtist) || this.OverwriteAlbumArtist)
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "AlbumArtist", tidalTrack.ArtistName, ref trackInfoUpdated, this.OverwriteTag);
        }
        if (string.IsNullOrWhiteSpace(track.Artist) || this.OverwriteArtist)
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "Artist",  tidalTrack.ArtistName, ref trackInfoUpdated, this.OverwriteTag);
        }
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "ARTISTS", artists, ref trackInfoUpdated, this.OverwriteTag);

        _mediaTagWriteService.UpdateTag(track, metadataInfo, "ISRC", tidalTrack.TrackISRC, ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "UPC", tidalTrack.AlbumUPC, ref trackInfoUpdated, this.OverwriteTag);
            
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Date", tidalTrack.ReleaseDate, ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Copyright", tidalTrack.Copyright, ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Disc Number", tidalTrack.DiscNumber.ToString(), ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Track Number", tidalTrack.TrackNumber.ToString(), ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Total Tracks", tidalTrack.TotalTracks.ToString(), ref trackInfoUpdated, this.OverwriteTag);

        
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