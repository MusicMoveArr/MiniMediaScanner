using ATL;
using Microsoft.Extensions.Caching.Memory;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models;
using MiniMediaScanner.Models.Deezer;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class TagMissingDeezerMetadataCommandHandler
{
    private readonly MetadataRepository _metadataRepository;
    private readonly ArtistRepository _artistRepository;
    private readonly MediaTagWriteService _mediaTagWriteService;
    private readonly ImportCommandHandler _importCommandHandler;
    private readonly MatchRepository _matchRepository;
    private readonly DeezerRepository _deezerRepository;
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

    public TagMissingDeezerMetadataCommandHandler(string connectionString)
    {
        _metadataRepository = new MetadataRepository(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
        _mediaTagWriteService = new MediaTagWriteService();
        _importCommandHandler = new ImportCommandHandler(connectionString);
        _matchRepository = new MatchRepository(connectionString);
        _deezerRepository = new DeezerRepository(connectionString);
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
        long? deezerArtistId = await _matchRepository.GetBestDeezerMatchAsync(metadata.ArtistId.Value, metadata.ArtistName);
        
        if (!deezerArtistId.HasValue)
        {
            Console.WriteLine($"Nothing found for '{metadata.AlbumName}', '{metadata.Title}'");
            return;
        }
        
        var allDeezerTracks = new List<TrackScoreComparerDeezerModel>();
        string deezerArtistTracksKey = $"DeezerArtistTracks_{deezerArtistId}";
        using (await _asyncLock.LockAsync())
        {
            if (!_cache.TryGetValue(deezerArtistTracksKey, out List<TrackScoreComparerDeezerModel>? result))
            {
                var tempDeezerTracks = await _deezerRepository.GetTrackByArtistIdAsync(deezerArtistId.Value, string.Empty, string.Empty);
                allDeezerTracks.AddRange(tempDeezerTracks
                    .Select(model => new TrackScoreComparerDeezerModel(model)));
                
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
        
        var deezerScoreResult = _trackScoreService
            .GetFirstTrackScore(trackScoreTargetModel, allDeezerTracks, 80);
        
        if (deezerScoreResult == null)
        {
            Console.WriteLine($"Nothing found for '{metadata.AlbumName}', '{metadata.Title}'");
            return;
        }
        
        DeezerTrackDbModel deezerTrack = ((TrackScoreComparerDeezerModel)deezerScoreResult.TrackScoreComparer).TrackModel;
        var trackArtists = await _deezerRepository.GetTrackArtistsAsync(deezerTrack.TrackId, deezerArtistId.Value);
        string artists = string.Join(';', trackArtists);
        
        Console.WriteLine($"Release found for '{metadata.Path}', Title '{deezerTrack.TrackName}', Date '{deezerTrack.AlbumReleaseDate}'");

        Track track = new Track(metadata.Path);
        var metadataInfo = await _fileMetaDataService.GetMetadataInfoAsync(new FileInfo(track.Path));
        bool trackInfoUpdated = false;
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Track Id", deezerTrack.TrackId.ToString(), ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Track Explicit", deezerTrack.ExplicitLyrics ? "Y": "N", ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Track Href", deezerTrack.TrackHref, ref trackInfoUpdated, this.OverwriteTag);
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Album Id", deezerTrack.AlbumId.ToString(), ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Album Href", deezerTrack.AlbumHref, ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Album Release Date", deezerTrack.AlbumReleaseDate, ref trackInfoUpdated, this.OverwriteTag);
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Artist Id", deezerTrack.ArtistId.ToString(), ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Artist Href", deezerTrack.ArtistHref, ref trackInfoUpdated, this.OverwriteTag);
        
        if (string.IsNullOrWhiteSpace(track.Title) || this.OverwriteTrack)
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "Title", deezerTrack.TrackName, ref trackInfoUpdated, this.OverwriteTag);
        }
        if (string.IsNullOrWhiteSpace(track.Album) || this.OverwriteAlbum)
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "Album", deezerTrack.AlbumName, ref trackInfoUpdated, this.OverwriteTag);
        }
        if (string.IsNullOrWhiteSpace(track.AlbumArtist)  || this.OverwriteAlbumArtist)
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "AlbumArtist", deezerTrack.ArtistName, ref trackInfoUpdated, this.OverwriteTag);
        }
        if (string.IsNullOrWhiteSpace(track.Artist) || this.OverwriteArtist)
        {
            _mediaTagWriteService.UpdateTag(track, metadataInfo, "Artist",  deezerTrack.ArtistName, ref trackInfoUpdated, this.OverwriteTag);
        }
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "ARTISTS", artists, ref trackInfoUpdated, this.OverwriteTag);

        _mediaTagWriteService.UpdateTag(track, metadataInfo, "ISRC", deezerTrack.TrackISRC, ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "UPC", deezerTrack.AlbumUPC, ref trackInfoUpdated, this.OverwriteTag);
            
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Date", deezerTrack.AlbumReleaseDate, ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Label", deezerTrack.Label, ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Disc Number", deezerTrack.DiscNumber.ToString(), ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Track Number", deezerTrack.TrackPosition.ToString(), ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Total Tracks", deezerTrack.AlbumTotalTracks.ToString(), ref trackInfoUpdated, this.OverwriteTag);

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