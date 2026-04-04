using MiniMediaScanner.Callbacks.Status;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;
using Spectre.Console;

namespace MiniMediaScanner.Commands;

public class UpdateSoundCloudCommandHandler
{
    private readonly UpdateSoundCloudRepository _soundCloudRepository;
    private readonly SoundCloudService _soundCloudService;
    public UpdateSoundCloudCommandHandler(string connectionString, 
        string soundCloudClientId, 
        int preventUpdateWithinDays)
    {
        _soundCloudRepository = new UpdateSoundCloudRepository(connectionString);
        _soundCloudService = new SoundCloudService(connectionString, soundCloudClientId, preventUpdateWithinDays);
    }
    
    public async Task UpdateSoundCloudArtistsByNameAsync(string artistName)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(Markup.Escape($"Updating SoundCloud Artist '{artistName}'"), async ctx =>
            {
                while (true)
                {
                    try
                    {
                        await _soundCloudService.UpdateArtistByNameAsync(artistName, callback =>
                        {
                            if (callback.Status == UpdateSoundCloudStatus.Updating)
                            {
                                if (string.IsNullOrWhiteSpace(callback.ExtraInfo))
                                {
                                    AnsiConsole.WriteLine(Markup.Escape($"Importing Album '{callback.PlaylistName}', Artist '{callback.UserName}'"));
                                }
                                ctx.Status(Markup.Escape($"Updating SoundCloud Artist '{callback.UserName}' Albums {callback.Progress} of {callback.PlaylistCount}{callback.ExtraInfo}"));
                            }
                            else if(callback.Status == UpdateSoundCloudStatus.SkippedSyncedWithin)
                            {
                                AnsiConsole.WriteLine(Markup.Escape($"Skipped synchronizing for SoundCloud ArtistId '{callback?.UserId}' synced already within {_soundCloudService.PreventUpdateWithinDays}days"));
                            }
                        });
                        break;
                    }
                    catch (SoundCloudExplode.Exceptions.RequestLimitExceededException e)
                    {
                        Console.WriteLine(e.Message + $", {DateTime.Now:dd-mm-yyyy HH:mm:ss}");
                        Thread.Sleep(TimeSpan.FromSeconds(15));
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
    
    public async Task UpdateAllSoundCloudArtistsAsync()
    {
        Console.WriteLine("Not implemented yet");
    }
}