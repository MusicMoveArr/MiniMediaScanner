using MiniMediaScanner.Callbacks;
using MiniMediaScanner.Callbacks.Status;
using MiniMediaScanner.Models.Spotify;
using MiniMediaScanner.Repositories;
using RestSharp;
using SpotifyAPI.Web;

namespace MiniMediaScanner.Services;

public class SpotifyService
{
    public readonly int PreventUpdateWithinDays; 
    private readonly string _connectionString;
    private readonly UpdateSpotifyRepository _updateSpotifyRepository;
    private readonly SpotifyAPICacheLayerService _cacheLayerService;

    public SpotifyService(string spotifyClientId, 
        string spotifySecretId, 
        string connectionString, 
        int apiDelay,
        int preventUpdateWithinDays)
    {
        this.PreventUpdateWithinDays = preventUpdateWithinDays;
        _connectionString = connectionString;
        _updateSpotifyRepository = new UpdateSpotifyRepository(_connectionString);
        _cacheLayerService = new SpotifyAPICacheLayerService(apiDelay, spotifyClientId, spotifySecretId);
    }
    
    public async Task UpdateArtistByNameAsync(string artistName, 
            Action<UpdateSpotifyCallback>? callback = null)
    {
        await _cacheLayerService.AuthenticateAsync();
        
        var search = new SearchRequest(SearchRequest.Types.Artist, artistName);
        var searchResult = await _cacheLayerService.SpotifyClient.Search.Item(search);
        
        foreach(var artist in searchResult.Artists.Items
                   /* .Where(artist => string.Equals(artist.Name, artistName, StringComparison.OrdinalIgnoreCase))*/)
        {
            await UpdateArtistByIdAsync(artist.Id, artist, callback);
        }
    }

    public async Task UpdateArtistByIdAsync(string artistId, 
        FullArtist? artist = null, 
        Action<UpdateSpotifyCallback>? callback = null)
    {
        await _cacheLayerService.AuthenticateAsync();
        await _updateSpotifyRepository.SetConnectionAsync();

        try
        {
            DateTime? lastSyncTime = await _updateSpotifyRepository.GetArtistLastSyncTimeAsync(artistId);
            if (lastSyncTime?.Year > 2000 && DateTime.Now.Subtract(lastSyncTime.Value).TotalDays < PreventUpdateWithinDays)
            {
                await _updateSpotifyRepository.CommitAsync();
                callback?.Invoke(new UpdateSpotifyCallback(artist, UpdateSpotifyStatus.SkippedSyncedWithin));
                return;
            }

            if (artist == null)
            {
                artist = await _cacheLayerService.GetArtistAsync(artistId);
            }
            
            await _updateSpotifyRepository.UpsertArtistAsync(artist);
            await _updateSpotifyRepository.UpsertArtistImageAsync(artist);
            
            List<SimpleAlbum> simpleAlbums = new List<SimpleAlbum>();
            await foreach (var simpleAlbum in _cacheLayerService.SpotifyClient.Paginate(
                               await _cacheLayerService.SpotifyClient.Artists.GetAlbums(artistId)))
            {
                simpleAlbums.Add(simpleAlbum);
            }

            int progress = 1;

            foreach(var simpleAlbum in simpleAlbums)
            {
                callback?.Invoke(new UpdateSpotifyCallback(artist, simpleAlbum, simpleAlbums, UpdateSpotifyStatus.Updating, progress++));
                
                if (simpleAlbum.AlbumGroup == "appears_on")
                {
                    continue;
                }
                
                var album = await _cacheLayerService.GetAlbumAsync(simpleAlbum.Id);
                
                await _updateSpotifyRepository.UpsertAlbumAsync(album, simpleAlbum?.AlbumGroup ?? string.Empty, artistId);
                await _updateSpotifyRepository.UpsertAlbumArtistAsync(album);
                await _updateSpotifyRepository.UpsertAlbumImageAsync(album);
                await _updateSpotifyRepository.UpsertAlbumExternalIdAsync(album);
                
                TracksRequest req = new TracksRequest(album.Tracks.Items.Take(50).Select(track => track.Id).ToList());
                var fullTracks = await _cacheLayerService.SpotifyClient.Tracks.GetSeveral(req);

                if (await _updateSpotifyRepository.GetAlbumTrackCountAsync(simpleAlbum.Id) == fullTracks.Tracks.Count)
                {
                    continue;
                }
                
                foreach (var track in fullTracks.Tracks)
                {
                    await _updateSpotifyRepository.UpsertTrackAsync(track);
                    await _updateSpotifyRepository.UpsertTrack_ArtistAsync(track);
                    await _updateSpotifyRepository.UpsertTrackExternalIdAsync(track);

                    foreach (var artistAssociated in track.Artists)
                    {
                        var extraArtist = await _cacheLayerService.GetArtistAsync(artistAssociated.Id);
                        if (extraArtist != null)
                        {
                            await _updateSpotifyRepository.UpsertArtistAsync(extraArtist);
                            await _updateSpotifyRepository.UpsertArtistImageAsync(extraArtist);
                        }
                    }
                }
            }
            await _updateSpotifyRepository.SetArtistLastSyncTimeAsync(artistId);
            await _updateSpotifyRepository.CommitAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            await _updateSpotifyRepository.RollbackAsync();
        }
    }
}