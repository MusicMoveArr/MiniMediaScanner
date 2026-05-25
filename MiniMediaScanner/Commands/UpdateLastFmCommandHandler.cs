using ListRandomizer;
using MiniMediaScanner.Callbacks.Status;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;
using Spectre.Console;

namespace MiniMediaScanner.Commands;

public class UpdateLastFmCommandHandler
{
    private readonly LastFmRepository _lastFmRepository;
    private readonly LastFmService _lastFmService;

    public UpdateLastFmCommandHandler(
        string connectionString,
        string lastfmApiKey,
        string lastfmSharedSecret,
        int preventUpdateWithinDays,
        int maxAlbumCountToPull)
    {
        _lastFmRepository = new LastFmRepository(connectionString);
        _lastFmService = new LastFmService(connectionString, lastfmApiKey, lastfmSharedSecret, preventUpdateWithinDays, maxAlbumCountToPull);
    }
    
    public async Task UpdateLastFmArtistsByNameAsync(string artistName)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(Markup.Escape($"Updating Last.Fm Artist '{artistName}'"), async ctx =>
            {
                while (true)
                {
                    try
                    {
                        await _lastFmService.UpdateArtistByNameAsync(artistName, callback =>
                        {
                            if (callback.Status == UpdateLastFmStatus.Updating)
                            {
                                if (string.IsNullOrWhiteSpace(callback.ExtraInfo))
                                {
                                    AnsiConsole.WriteLine(Markup.Escape($"Importing Album '{callback.AlbumName}', Artist '{callback.ArtistName}'"));
                                }
                                ctx.Status(Markup.Escape($"Updating Last.Fm Artist '{callback.ArtistName}' Albums {callback.Progress} of {callback.AlbumCount}{callback.ExtraInfo}"));
                            }
                            else if(callback.Status == UpdateLastFmStatus.SkippedSyncedWithin)
                            {
                                AnsiConsole.WriteLine(Markup.Escape($"Skipped synchronizing for Last.Fm '{callback?.ArtistName}' synced already within {_lastFmService.PreventUpdateWithinDays}days"));
                            }
                        });
                        break;
                    }
                    catch (Npgsql.NpgsqlException e)
                    {
                        Console.WriteLine(e.Message + "\t\n" + e.StackTrace);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message + "\t\n" + e.StackTrace);
                        break;
                    }
                }
            });
    }
    
    public async Task UpdateAllLastFmArtistsAsync(bool updateNonpulledArtists)
    {
        var artistNames = new List<string>();

        if (updateNonpulledArtists)
        {
            artistNames.AddRange(await _lastFmRepository.GetNonpulledLastFmArtistNamesAsync());
        }
                
        var tempArtistNames = (await _lastFmRepository
            .GetAllLastFmArtistNamesAsync())
            .Except(artistNames)
            .ToList();
        
        tempArtistNames.Shuffle();
        artistNames.AddRange(tempArtistNames);
        
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(Markup.Escape($"Updating all Last.Fm Artist..."), async ctx =>
            {
                foreach (var artistName in artistNames)
                {
                    try
                    {
                        await _lastFmService.UpdateArtistByNameAsync(artistName, callback =>
                        {
                            if (callback.Status == UpdateLastFmStatus.Updating)
                            {
                                if (string.IsNullOrWhiteSpace(callback.ExtraInfo))
                                {
                                    AnsiConsole.WriteLine(Markup.Escape($"Importing Album '{callback.AlbumName}', Artist '{callback.ArtistName}'"));
                                }
                            
                                ctx.Status(Markup.Escape($"Updating Last.Fm Artist '{callback.ArtistName}' Albums {callback.Progress} of {callback.AlbumCount}{callback.ExtraInfo}"));
                            }
                            else if(callback.Status == UpdateLastFmStatus.SkippedSyncedWithin)
                            {
                                AnsiConsole.WriteLine(Markup.Escape($"Skipped synchronizing for Last.Fm '{callback.ArtistName}' synced already within {_lastFmService.PreventUpdateWithinDays}days"));
                            }
                        });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message + "\r\n\r\n" + e.StackTrace);
                    }
                }
            });
    }
}