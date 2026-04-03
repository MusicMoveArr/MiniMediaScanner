using System.Collections.Concurrent;
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
        await AnsiConsole.Progress()
            .HideCompleted(true)
            .AutoClear(true)
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn()
                {
                    Alignment = Justify.Left
                },
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
            })
            .StartAsync(async ctx =>
            {
                await _deezerService.PrepareProxiesAsync();
                
                var artistIds = await _deezerRepository.GetAllDeezerArtistIdsAsync();
                
                await ParallelHelper.ForEachAsync(artistIds, _threads, async artistId =>
                {
                    ConcurrentDictionary<string, ProgressTask> tasks = new ConcurrentDictionary<string, ProgressTask>();
                    
                    try
                    {
                        await _deezerService.UpdateArtistByIdAsync(artistId, callback =>
                        {
                            if (callback.Status == UpdateDeezerStatus.Updating)
                            {
                                if (string.IsNullOrWhiteSpace(callback.ExtraInfo))
                                {
                                    AnsiConsole.WriteLine(Markup.Escape($"Importing Album '{callback.AlbumName}', Artist '{callback.ArtistName}'"));
                                }
                                
                                if (!tasks.ContainsKey(callback.UpdateKey))
                                {
                                    tasks.TryAdd(callback.UpdateKey, ctx.AddTask(Markup.Escape($"Updating Deezer Artist '{callback.ArtistName}'")));
                                }
                                
                                ProgressTask? task = tasks[callback.UpdateKey];
                                
                                if (task != null)
                                {
                                    task.MaxValue = callback.AlbumCount;
                                    task.Value = callback.Progress;
                                    task.Description = Markup.Escape( $"Updating Deezer Artist '{callback.ArtistName}' Albums {callback.Progress} of {callback.AlbumCount}{callback.ExtraInfo}");
                                }
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
            });
    }
}