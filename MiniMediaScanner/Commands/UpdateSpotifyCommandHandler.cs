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
    
    public async Task UpdateSpotifyArtistsByNameAsync(string artist)
    {
        Console.WriteLine($"Updating artist, {artist}");

        var artistIds = await _spotifyRepository.GetSpotifyArtistIdsByNameAsync(artist);
        
        foreach (var artistId in artistIds)
        {
            DateTime? lastSyncTime = await _spotifyRepository.GetArtistLastSyncTimeAsync(artistId);

            if (lastSyncTime?.Year > 2000 && DateTime.Now.Subtract(lastSyncTime.Value).TotalDays < 7)
            {
                Console.WriteLine($"Skipped synchronizing for Spotify '{artist}' synced already within 7days");
                return;
            }
        }

        try
        {
            await _spotifyService.UpdateArtistByNameAsync(artist);
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
    
    public async Task UpdateAllSpotifyArtistsAsync()
    {
        var artists = await _artistRepository.GetAllArtistNamesAsync();
        foreach (var artist in artists)
        {
            try
            {
                await UpdateSpotifyArtistsByNameAsync(artist);
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
    
    public async Task UpdateSpotifyArtistIdAsync(string artistId)
    {
        try
        {
            Console.WriteLine($"Updating Music Spotify Artist Id '{artistId}'");
            await _spotifyService.UpdateArtistByIdAsync(artistId);
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