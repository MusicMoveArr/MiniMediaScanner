using MiniMediaScanner.Callbacks.Status;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;
using Spectre.Console;

namespace MiniMediaScanner.Commands;

public class UpdateDeezerCommandHandler
{
    private readonly DeezerRepository _deezerRepository;
    private readonly DeezerService _deezerService;
    private readonly int _threads;

    public UpdateDeezerCommandHandler(string connectionString, 
        string proxyFile, 
        string singleProxy, 
        string proxyMode, 
        bool saveTrackToken, 
        bool savePreviewUrl,
        int threads,
        int preventUpdateWithinDays)
    {
        _deezerService = new DeezerService(connectionString, proxyFile, singleProxy, proxyMode, saveTrackToken, savePreviewUrl, preventUpdateWithinDays);
        _deezerRepository = new DeezerRepository(connectionString);
        _threads = threads;
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
                        AnsiConsole.WriteLine(Markup.Escape($"Skipped synchronizing for Deezer ArtistId '{callback?.ArtistId}' synced already within {_deezerService.PreventUpdateWithinDays}days"));
                    }
                });
            });
    }
    
    public async Task UpdateAllDeezerArtistsAsync()
    {
        
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(Markup.Escape($"Updating all Deezer Artist..."), async ctx =>
            {
                await _deezerService.PrepareProxiesAsync();

                int offset = 0;

                while (true)
                {
                    var artistIds = await _deezerRepository.GetAllDeezerArtistIdsAsync(offset);
                    offset += DeezerRepository.PagingSize;

                    if (artistIds.Count == 0)
                    {
                        break;
                    }
                    
                    await ParallelHelper.ForEachAsync(artistIds, _threads, async artistId =>
                    {
                        try
                        {
                            await _deezerService.UpdateArtistByIdAsync(artistId, callback =>
                            {
                                if (callback.Status == UpdateDeezerStatus.Updating)
                                {
                                    if (string.IsNullOrWhiteSpace(callback.ExtraInfo))
                                    {
                                        AnsiConsole.WriteLine(Markup.Escape(
                                            $"Importing Album '{callback.AlbumName}', Artist '{callback.ArtistName}'"));
                                    }

                                    ctx.Status(Markup.Escape($"Updating Deezer Artist '{callback.ArtistName}' Albums {callback.Progress} of {callback.AlbumCount}{callback.ExtraInfo}"));
                                }
                                else if(callback.Status == UpdateDeezerStatus.SkippedSyncedWithin)
                                {
                                    AnsiConsole.WriteLine(Markup.Escape($"Skipped synchronizing for Deezer ArtistId '{callback?.ArtistId}' synced already within {_deezerService.PreventUpdateWithinDays}days"));
                                }
                            });
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    });
                }
            });
    }
}