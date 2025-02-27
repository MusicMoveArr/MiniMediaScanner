using MiniMediaScanner.Models.Spotify;
using MiniMediaScanner.Repositories;
using RestSharp;
using SpotifyAPI.Web;

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
    
    public void UpdateArtistByName(string artistName)
    {
        if (string.IsNullOrWhiteSpace(_spotifyAuthentication?.AccessToken) ||
            (_spotifyAuthentication.ExpiresIn > 0 && DateTime.Now > _spotifyAuthentication.ExpiresAt))
        {
            _spotifyAuthentication = GetToken();
        }

        var spotify = new SpotifyClient(_spotifyAuthentication.AccessToken);
        var search = new SearchRequest(SearchRequest.Types.Artist, artistName);
        var searchResult = spotify.Search.Item(search).GetAwaiter().GetResult();
        Thread.Sleep(TimeSpan.FromSeconds(_apiDelay));
        
        foreach(var artist in searchResult.Artists.Items
                    .Where(artist => string.Equals(artist.Name, artistName, StringComparison.OrdinalIgnoreCase)))
        {
            UpdateArtistById(artist.Id, artist);
        }
    }

    public void UpdateArtistById(string artistId, FullArtist artist = null)
    {
        if (string.IsNullOrWhiteSpace(_spotifyAuthentication?.AccessToken) ||
            (_spotifyAuthentication.ExpiresIn > 0 && DateTime.Now > _spotifyAuthentication.ExpiresAt))
        {
            _spotifyAuthentication = GetToken();
        }
        
        DateTime? lastSyncTime = _spotifyRepository.GetArtistLastSyncTime(artistId);

        if (lastSyncTime?.Year > 2000 && DateTime.Now.Subtract(lastSyncTime.Value).TotalDays < 7)
        {
            Console.WriteLine($"Skipped synchronizing for Spotify '{artistId}' synced already within 7days");
            return;
        }
            
        var spotify = new SpotifyClient(_spotifyAuthentication.AccessToken);

        if (artist == null)
        {
            artist = spotify.Artists.Get(artistId).GetAwaiter().GetResult();
            Thread.Sleep(TimeSpan.FromSeconds(_apiDelay));
        }
        
        var albums = spotify.Artists.GetAlbums(artistId).GetAwaiter().GetResult();
        Thread.Sleep(TimeSpan.FromSeconds(_apiDelay));
        
        _spotifyRepository.InsertOrUpdateArtist(artist);
        _spotifyRepository.InsertOrUpdateArtistImage(artist);

        AlbumsRequest albumsRequest = new AlbumsRequest(albums.Items.Take(20).Select(album => album.Id).ToList());
        var fullAlbums = spotify.Albums.GetSeveral(albumsRequest).GetAwaiter().GetResult();
        Thread.Sleep(TimeSpan.FromSeconds(_apiDelay));

        foreach (var album in fullAlbums.Albums)
        {
            Console.WriteLine($"Grabbing album {album.Name}, Artist: {artist.Name}");
            var simpleAlbum = albums.Items.FirstOrDefault(album => album.Id == album.Id);
            _spotifyRepository.InsertOrUpdateAlbum(album, simpleAlbum?.AlbumGroup ?? string.Empty);
            _spotifyRepository.InsertOrUpdateAlbumArtist(album);
            _spotifyRepository.InsertOrUpdateAlbumImage(album);
            _spotifyRepository.InsertOrUpdateAlbumExternalId(album);
                
            TracksRequest req = new TracksRequest(album.Tracks.Items.Take(50).Select(track => track.Id).ToList());
            var fullTracks = spotify.Tracks.GetSeveral(req).GetAwaiter().GetResult();
            Thread.Sleep(TimeSpan.FromSeconds(_apiDelay));
                
            foreach (var track in fullTracks.Tracks)
            {
                _spotifyRepository.InsertOrUpdateTrack(track);
                _spotifyRepository.InsertOrUpdateTrack_Artist(track);
                _spotifyRepository.InsertOrUpdateTrackExternalId(track);
            }
        }
    }

    private SpotifyAuthenticationResponse GetToken()
    {
        var client = new RestClient("https://accounts.spotify.com/api/token");
        var request = new RestRequest();
        request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
        request.AddParameter("grant_type", "client_credentials");
        request.AddParameter("client_id", _spotifyClientId);
        request.AddParameter("client_secret", _spotifySecretId);
        
        var response = client.Post<SpotifyAuthenticationResponse>(request);
        response.RequestedAt = DateTime.Now;
        return response;
    }
}