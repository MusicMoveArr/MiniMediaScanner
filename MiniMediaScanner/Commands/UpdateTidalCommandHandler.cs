using MiniMediaScanner.Callbacks.Status;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;
using Spectre.Console;

namespace MiniMediaScanner.Commands;

public class UpdateTidalCommandHandler
{
    private readonly TidalRepository _tidalRepository;
    private readonly TidalService _tidalService;

    public UpdateTidalCommandHandler(string connectionString, 
        string clientId, 
        string clientSecret, 
        string countryCode)
    {
        _tidalRepository = new TidalRepository(connectionString);
        _tidalService = new TidalService(connectionString, clientId, clientSecret, countryCode);
    }
    
    public async Task UpdateTidalArtistsByNameAsync(string artistName)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(Markup.Escape($"Updating Tidal Artist '{artistName}'"), async ctx =>
            {
                await _tidalService.UpdateArtistByNameAsync(artistName, callback =>
                {
                    if (callback.Status == UpdateTidalStatus.Updating)
                    {
                        AnsiConsole.WriteLine(Markup.Escape($"Importing Album '{callback.AlbumName}', Artist '{callback.ArtistName}'"));
                        ctx.Status(Markup.Escape($"Updating Tidal Artist '{callback.ArtistName}' Albums {callback.Progress} of {callback.AlbumCount}"));
                    }
                    else if(callback.Status == UpdateTidalStatus.SkippedSyncedWithin)
                    {
                        AnsiConsole.WriteLine(Markup.Escape($"Skipped synchronizing for Tidal ArtistId '{callback?.ArtistId}' synced already within 7days"));
                    }
                });
            });
    }
    
    public async Task UpdateAllTidalArtistsAsync()
    {
        var artistIds = await _tidalRepository.GetAllTidalArtistIdsAsync();
        
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(Markup.Escape($"Updating all Tidal Artist..."), async ctx =>
            {
                foreach (var artistId in artistIds)
                {
                    await _tidalService.UpdateArtistByIdAsync(artistId, callback =>
                    {
                        if (callback.Status == UpdateTidalStatus.Updating)
                        {
                            AnsiConsole.WriteLine(Markup.Escape($"Importing Album '{callback.AlbumName}', Artist '{callback.ArtistName}'"));
                            ctx.Status(Markup.Escape($"Updating Tidal Artist '{callback.ArtistName}' Albums {callback.Progress} of {callback.AlbumCount}"));
                        }
                        else if(callback.Status == UpdateTidalStatus.SkippedSyncedWithin)
                        {
                            AnsiConsole.WriteLine(Markup.Escape($"Skipped synchronizing for Tidal ArtistId '{callback?.ArtistId}' synced already within 7days"));
                        }
                    });
                }
            });
    }
}