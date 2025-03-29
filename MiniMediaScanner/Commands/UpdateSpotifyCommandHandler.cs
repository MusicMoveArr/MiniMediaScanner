using System.Diagnostics;
using MiniMediaScanner.Callbacks.Status;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;
using Spectre.Console;
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
        try
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"Importing '{artist}'", async ctx => 
                {
                    await _spotifyService.UpdateArtistByNameAsync(artist, callback =>
                    {
                        if (callback.Status == UpdateSpotifyStatus.Updating)
                        {
                            AnsiConsole.WriteLine(Markup.Escape($"Importing Album '{callback.CurrentAblum?.Name}', Artist '{callback.Artist?.Name}'"));
                            ctx.Status(Markup.Escape($"Importing Artist '{callback.Artist?.Name}' Albums {callback.Progress} of {callback.Albums?.Count}"));
                        }
                        else if(callback.Status == UpdateSpotifyStatus.SkippedSyncedWithin)
                        {
                            AnsiConsole.WriteLine(Markup.Escape($"Skipped synchronizing for Spotify '{callback?.Artist?.Name}' synced already within 7days"));
                        }
                    });
                });
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
}