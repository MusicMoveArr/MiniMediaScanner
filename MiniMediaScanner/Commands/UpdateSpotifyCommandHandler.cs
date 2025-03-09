using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;
using SpotifyAPI.Web;

namespace MiniMediaScanner.Commands;

public class UpdateSpotifyCommandHandler
{
    private readonly SpotifyService _spotifyService;
    private readonly SpotifyRepository _spotifyRepository;
    private readonly ArtistRepository _artistRepository;
    public UpdateSpotifyCommandHandler(string connectionString, 
        string spotifyClientId,
        string spotifySecretId,
        int apiDelay)
    {
        _spotifyService = new SpotifyService(spotifyClientId, spotifySecretId, connectionString, apiDelay);
        _spotifyRepository = new SpotifyRepository(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
    }
    
    public void UpdateSpotifyArtistsByName(string artist)
    {
        Console.WriteLine($"Updating artist, {artist}");

        var artistIds = _spotifyRepository.GetSpotifyArtistIdsByName(artist);
        
        foreach (var artistId in artistIds)
        {
            DateTime? lastSyncTime = _spotifyRepository.GetArtistLastSyncTime(artistId);

            if (lastSyncTime?.Year > 2000 && DateTime.Now.Subtract(lastSyncTime.Value).TotalDays < 7)
            {
                Console.WriteLine($"Skipped synchronizing for Spotify '{artist}' synced already within 7days");
                return;
            }
        }

        try
        {
            _spotifyService.UpdateArtistByName(artist);
        }
        catch (APITooManyRequestsException ex)
        {
            Console.WriteLine($"Too many requests to synced artist, waiting {ex.RetryAfter}...");
            Thread.Sleep(ex.RetryAfter.Add(TimeSpan.FromSeconds(10)));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;  
        }
    }
    
    public void UpdateAllSpotifyArtists()
    {
        var artists = _artistRepository.GetAllArtistNames();
        foreach (var artist in artists)
        {
            try
            {
                UpdateSpotifyArtistsByName(artist);
            }
            catch (APITooManyRequestsException ex)
            {
                Console.WriteLine($"Too many requests to synced artist, waiting {ex.RetryAfter}...");
                Thread.Sleep(ex.RetryAfter.Add(TimeSpan.FromSeconds(10)));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
    
    public void UpdateSpotifyArtistId(string artistId)
    {
        try
        {
            Console.WriteLine($"Updating Music Spotify Artist Id '{artistId}'");
            _spotifyService.UpdateArtistById(artistId);
        }
        catch (APITooManyRequestsException ex)
        {
            Console.WriteLine($"Too many requests to synced artist, waiting {ex.RetryAfter}...");
            Thread.Sleep(ex.RetryAfter.Add(TimeSpan.FromSeconds(10)));
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}