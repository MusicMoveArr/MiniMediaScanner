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
    private readonly TrackScoreService _trackScoreService;
    
    public bool Confirm { get; set; }
    public bool OverwriteTag { get; set; }
    public bool OverwriteArtist { get; set; }
    public bool OverwriteAlbumArtist { get; set; }
    public bool OverwriteAlbum { get; set; }
    public bool OverwriteTrack { get; set; }
    
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
        _trackScoreService = new TrackScoreService();
    }
    
    public async Task TagMetadataAsync()
    {
        foreach (var artist in await _artistRepository.GetAllArtistNamesAsync())
        {
            await TagMetadataAsync(artist, string.Empty);
        }
    }

    public async Task TagMetadataAsync( string artist, string album)
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
                await ProcessAlumGroupAsync(record.ToList(), artist, record.First().AlbumName!);
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

        var allDeezerTracks = new List<TrackScoreComparerDeezerModel>();
        string deezerArtistTracksKey = $"DeezerArtistTracks_{deezerArtistId}";
        using (await _asyncLock.LockAsync())
        {
            if (!_cache.TryGetValue(deezerArtistTracksKey, out List<TrackScoreComparerDeezerModel>? result))
            {
                var tempDeezerTracks = await _deezerRepository.GetTrackByArtistIdAsync(deezerArtistId, string.Empty, string.Empty);
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
        
        var trackScoreTargetModels = metadataModels
            .Select(track => new TrackScoreTargetModel
            {
                MetadataId = track.MetadataId!.Value,
                Artist = track.ArtistName ?? string.Empty,
                Album = track.AlbumName ?? string.Empty,
                AlbumId = track.AlbumId?.ToString() ?? string.Empty,
                Date = track.Tag_Date ?? string.Empty,
                Isrc = track.Tag_Isrc ?? string.Empty,
                Title = track.Title ?? string.Empty,
                Duration = track.TrackLength,
                TrackNumber = track.Tag_Track,
                TrackTotalCount = track.Tag_TrackCount,
                Upc = track.Tag_Upc ?? string.Empty
            })
            .ToList();
        
        var trackList = _trackScoreService
            .GetAllTrackScore(trackScoreTargetModels, allDeezerTracks, 80)
            .GroupBy(tracks => tracks.TrackScoreComparer.AlbumId)
            .OrderByDescending(tracks => tracks.Count());

        List<Guid> processedMetadataIds = new List<Guid>();
        foreach (var groupedTracks in trackList)
        {
            foreach (var matchedTrack in groupedTracks)
            {
                if (processedMetadataIds.Contains(matchedTrack.TrackScore.MetadataId))
                {
                    continue;
                }

                var metadataModel = metadataModels
                    .FirstOrDefault(m => m.MetadataId == matchedTrack.TrackScore.MetadataId);
                
                try
                {
                    DeezerTrackDbModel deezerTrackModel = ((TrackScoreComparerDeezerModel)matchedTrack.TrackScoreComparer).TrackModel;
                    
                    Console.WriteLine($"Processing file '{metadataModel.Path}'");
                    await ProcessFileAsync(metadataModel, deezerTrackModel, deezerArtistId);
                    processedMetadataIds.Add(metadataModel.MetadataId.Value);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
    }

    private async Task ProcessFileAsync(
        MetadataModel metadata,
        DeezerTrackDbModel deezerTrack,
        long deezerArtistId)
    {
        Track track = new Track(metadata.Path);
        var metadataInfo = _fileMetaDataService.GetMetadataInfo(track);
        
        bool trackInfoUpdated = false;
        var trackArtists = await _deezerRepository.GetTrackArtistsAsync(deezerTrack.TrackId, deezerArtistId);
        string artists = string.Join(';', trackArtists);

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
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Track Id", deezerTrack.TrackId.ToString(), ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Track Explicit", deezerTrack.ExplicitLyrics ? "Y": "N", ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Track Href", deezerTrack.TrackHref, ref trackInfoUpdated, this.OverwriteTag);
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Album Id", deezerTrack.AlbumId.ToString(), ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Album Href", deezerTrack.AlbumHref, ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Album Release Date", deezerTrack.AlbumReleaseDate, ref trackInfoUpdated, this.OverwriteTag);
        
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Artist Id", deezerTrack.ArtistId.ToString(), ref trackInfoUpdated, this.OverwriteTag);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "Deezer Artist Href", deezerTrack.ArtistHref, ref trackInfoUpdated, this.OverwriteTag);
        
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