using MiniMediaScanner.Models.Spotify;
using MiniMediaScanner.Repositories;
using RestSharp;
using SpotifyAPI.Web;
using Swan;

namespace MiniMediaScanner.Services;

public class SpotifyService
{
    private readonly string _spotifyClientId;
    private readonly string _spotifySecretId;
    private static SpotifyAuthenticationResponse _spotifyAuthentication;
    private readonly string _connectionString;
    private SpotifyRepository _spotifyRepository;
    private int _apiDelay;

    public SpotifyService(string spotifyClientId, 
        string spotifySecretId, 
        string connectionString, 
        int apiDelay)
    {
        _spotifyClientId = spotifyClientId;
        _spotifySecretId = spotifySecretId;
        _connectionString = connectionString;
        _spotifyRepository = new SpotifyRepository(_connectionString);
        _apiDelay = apiDelay;
    }
    
    public async Task UpdateArtistByNameAsync(string artistName)
    {
        if (string.IsNullOrWhiteSpace(_spotifyAuthentication?.AccessToken) ||
            (_spotifyAuthentication.ExpiresIn > 0 && DateTime.Now > _spotifyAuthentication.ExpiresAt))
        {
            _spotifyAuthentication = await GetTokenAsync();
        }

        var spotify = new SpotifyClient(_spotifyAuthentication.AccessToken);
        var search = new SearchRequest(SearchRequest.Types.Artist, artistName);
        var searchResult = await spotify.Search.Item(search);
        Thread.Sleep(TimeSpan.FromSeconds(_apiDelay));
        
        foreach(var artist in searchResult.Artists.Items
                    .Where(artist => string.Equals(artist.Name, artistName, StringComparison.OrdinalIgnoreCase)))
        {
            await UpdateArtistByIdAsync(artist.Id, artist);
        }
    }

    public async Task UpdateArtistByIdAsync(string artistId, FullArtist? artist = null)
    {
        if (string.IsNullOrWhiteSpace(_spotifyAuthentication?.AccessToken) ||
            (_spotifyAuthentication.ExpiresIn > 0 && DateTime.Now > _spotifyAuthentication.ExpiresAt))
        {
            _spotifyAuthentication = await GetTokenAsync();
        }
        
        DateTime? lastSyncTime = await _spotifyRepository.GetArtistLastSyncTimeAsync(artistId);

        if (lastSyncTime?.Year > 2000 && DateTime.Now.Subtract(lastSyncTime.Value).TotalDays < 7)
        {
            Console.WriteLine($"Skipped synchronizing for Spotify '{artistId}' synced already within 7days");
            return;
        }
            
        var spotify = new SpotifyClient(_spotifyAuthentication.AccessToken);

        if (artist == null)
        {
            artist = await spotify.Artists.Get(artistId);
            Thread.Sleep(TimeSpan.FromSeconds(_apiDelay));
        }
        
        Thread.Sleep(TimeSpan.FromSeconds(_apiDelay));
        await _spotifyRepository.InsertOrUpdateArtistAsync(artist);
        await _spotifyRepository.InsertOrUpdateArtistImageAsync(artist);

        await foreach(var simpleAlbum in spotify.Paginate(await spotify.Artists.GetAlbums(artistId)))
        {
            if (simpleAlbum.AlbumGroup == "appears_on")
            {
                continue;
            }

            if (await _spotifyRepository.SpotifyAlbumIdExistsAsync(simpleAlbum.Id))
            {
                continue;
            }
            
            var album = await spotify.Albums.Get(simpleAlbum.Id);
            Thread.Sleep(TimeSpan.FromSeconds(_apiDelay));
            
            Console.WriteLine($"Grabbing album {album.Name}, Artist: {artist.Name}");
            await _spotifyRepository.InsertOrUpdateAlbumAsync(album, simpleAlbum?.AlbumGroup ?? string.Empty);
            await _spotifyRepository.InsertOrUpdateAlbumArtistAsync(album);
            await _spotifyRepository.InsertOrUpdateAlbumImageAsync(album);
            await _spotifyRepository.InsertOrUpdateAlbumExternalIdAsync(album);
                
            TracksRequest req = new TracksRequest(album.Tracks.Items.Take(50).Select(track => track.Id).ToList());
            var fullTracks = await spotify.Tracks.GetSeveral(req);
            Thread.Sleep(TimeSpan.FromSeconds(_apiDelay));
                
            foreach (var track in fullTracks.Tracks)
            {
                await _spotifyRepository.InsertOrUpdateTrackAsync(track);
                await _spotifyRepository.InsertOrUpdateTrack_ArtistAsync(track);
                await _spotifyRepository.InsertOrUpdateTrackExternalIdAsync(track);
            }
        }
    }

    private async Task<SpotifyAuthenticationResponse> GetTokenAsync()
    {
        using var client = new RestClient("https://accounts.spotify.com/api/token");
        var request = new RestRequest();
        request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
        request.AddParameter("grant_type", "client_credentials");
        request.AddParameter("client_id", _spotifyClientId);
        request.AddParameter("client_secret", _spotifySecretId);
        
        var response = await client.PostAsync<SpotifyAuthenticationResponse>(request);
        response.RequestedAt = DateTime.Now;
        return response;
    }
}