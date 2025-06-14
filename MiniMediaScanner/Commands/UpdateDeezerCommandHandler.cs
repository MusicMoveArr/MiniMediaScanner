using MiniMediaScanner.Callbacks.Status;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;
using Spectre.Console;

namespace MiniMediaScanner.Commands;

public class UpdateDeezerCommandHandler
{
    private readonly DeezerRepository _deezerRepository;
    private readonly DeezerService _deezerService;

    public UpdateDeezerCommandHandler(string connectionString, string proxyFile, string singleProxy, string proxyMode)
    {
        _deezerRepository = new DeezerRepository(connectionString);
        _deezerService = new DeezerService(connectionString, proxyFile, singleProxy, proxyMode);
    }
    
    public async Task UpdateDeezerArtistsByNameAsync(string artistName)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(Markup.Escape($"Updating Deezer Artist '{artistName}'"), async ctx =>
            {
                await _deezerService.UpdateArtistByNameAsync(artistName, callback =>
                {
                    if (callback.Status == UpdateDeezerStatus.Updating)
                    {
                        if (string.IsNullOrWhiteSpace(callback.ExtraInfo))
                        {
                            AnsiConsole.WriteLine(Markup.Escape($"Importing Album '{callback.AlbumName}', Artist '{callback.ArtistName}'"));
                        }
                        ctx.Status(Markup.Escape($"Updating Deezer Artist '{callback.ArtistName}' Albums {callback.Progress} of {callback.AlbumCount}{callback.ExtraInfo}"));
                    }
                    else if(callback.Status == UpdateDeezerStatus.SkippedSyncedWithin)
                    {
                        AnsiConsole.WriteLine(Markup.Escape($"Skipped synchronizing for Deezer ArtistId '{callback?.ArtistId}' synced already within 7days"));
                    }
                });
            });
    }
    
    public async Task UpdateAllDeezerArtistsAsync()
    {
        var artistIds = await _deezerRepository.GetAllDeezerArtistIdsAsync();
        
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(Markup.Escape($"Updating all Deezer Artist..."), async ctx =>
            {
                foreach (var artistId in artistIds)
                {
                    await _deezerService.UpdateArtistByIdAsync(artistId, callback =>
                    {
                        if (callback.Status == UpdateDeezerStatus.Updating)
                        {
                            if (string.IsNullOrWhiteSpace(callback.ExtraInfo))
                            {
                                AnsiConsole.WriteLine(Markup.Escape($"Importing Album '{callback.AlbumName}', Artist '{callback.ArtistName}'"));
                            }
                            
                            ctx.Status(Markup.Escape($"Updating Deezer Artist '{callback.ArtistName}' Albums {callback.Progress} of {callback.AlbumCount}{callback.ExtraInfo}"));
                        }
                        else if(callback.Status == UpdateDeezerStatus.SkippedSyncedWithin)
                        {
                            AnsiConsole.WriteLine(Markup.Escape($"Skipped synchronizing for Deezer ArtistId '{callback?.ArtistId}' synced already within 7days"));
                        }
                    });
                }
            });
    }
}