using ListRandomizer;
using MiniMediaScanner.Callbacks.Status;
using MiniMediaScanner.Models.Tidal;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;
using Spectre.Console;

namespace MiniMediaScanner.Commands;

public class UpdateTidalCommandHandler
{
    private readonly TidalRepository _tidalRepository;
    private readonly TidalService _tidalService;

    public UpdateTidalCommandHandler(string connectionString, 
        List<TidalTokenClientSecret> secretTokens, 
        string countryCode, 
        string proxyFile, 
        string singleProxy, 
        string proxyMode,
        int preventUpdateWithinDays)
    {
        _tidalRepository = new TidalRepository(connectionString);
        _tidalService = new TidalService(connectionString, secretTokens, countryCode, proxyFile, singleProxy, proxyMode, preventUpdateWithinDays);
    }
    
    public async Task UpdateTidalArtistsByNameAsync(string artistName)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(Markup.Escape($"Updating Tidal Artist '{artistName}'"), async ctx =>
            {
                try
                {
                    await _tidalService.UpdateArtistByNameAsync(artistName, callback =>
                    {
                        if (callback.Status == UpdateTidalStatus.Updating)
                        {
                            if (string.IsNullOrWhiteSpace(callback.ExtraInfo))
                            {
                                AnsiConsole.WriteLine(Markup.Escape($"Importing Album '{callback.AlbumName}', Artist '{callback.ArtistName}'"));
                            }
                            ctx.Status(Markup.Escape($"Updating Tidal Artist '{callback.ArtistName}' Albums {callback.Progress} of {callback.AlbumCount}{callback.ExtraInfo}"));
                        }
                        else if(callback.Status == UpdateTidalStatus.SkippedSyncedWithin)
                        {
                            AnsiConsole.WriteLine(Markup.Escape($"Skipped synchronizing for Tidal ArtistId '{callback?.ArtistId}' synced already within {_tidalService.PreventUpdateWithinDays}days"));
                        }
                    });
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message + "\t\n" + e.StackTrace);
                }
            });
    }
    
    public async Task UpdateAllTidalArtistsAsync()
    {
        var artistIds = await _tidalRepository.GetAllTidalArtistIdsAsync();
        artistIds.Shuffle();
        
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
                            if (string.IsNullOrWhiteSpace(callback.ExtraInfo))
                            {
                                AnsiConsole.WriteLine(Markup.Escape($"Importing Album '{callback.AlbumName}', Artist '{callback.ArtistName}'"));
                            }
                            
                            ctx.Status(Markup.Escape($"Updating Tidal Artist '{callback.ArtistName}' Albums {callback.Progress} of {callback.AlbumCount}{callback.ExtraInfo}"));
                        }
                        else if(callback.Status == UpdateTidalStatus.SkippedSyncedWithin)
                        {
                            AnsiConsole.WriteLine(Markup.Escape($"Skipped synchronizing for Tidal ArtistId '{callback?.ArtistId}' synced already within {_tidalService.PreventUpdateWithinDays}days"));
                        }
                    });
                }
            });
    }
}