using System.Diagnostics;
using FuzzySharp;
using MiniMediaScanner.Callbacks.Status;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;
using Spectre.Console;

namespace MiniMediaScanner.Commands;

public class UpdateMBCommandHandler
{
    private readonly MusicBrainzService _musicBrainzService;
    private readonly MusicBrainzArtistRepository _musicBrainzArtistRepository;
    private readonly MusicBrainzAPIService _musicBrainzAPIService;

    public UpdateMBCommandHandler(string connectionString, int preventUpdateWithinDays)
    {
        _musicBrainzService = new MusicBrainzService(connectionString, preventUpdateWithinDays);
        _musicBrainzArtistRepository = new MusicBrainzArtistRepository(connectionString);
        _musicBrainzAPIService = new MusicBrainzAPIService();
    }
    
    public async Task UpdateMusicBrainzArtistsByNameAsync(string artistName)
    {
        var artistIds = await _musicBrainzArtistRepository.GetArtistIdsByNameAsync(artistName);

        if (artistIds?.Count == 0)
        {
            artistIds = (await _musicBrainzAPIService
                .SearchArtistAsync(artistName))
                ?.Artists?
                .Where(artist => Fuzz.Ratio(artist.Name.ToLower(), artistName.ToLower()) > 80)
                .Select(artist => artist.Id)
                .ToList();
        }

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(Markup.Escape($"Updating Music Brainz Artist '{artistName}'"), async ctx =>
            {
                foreach (var artistId in artistIds)
                {
                    await _musicBrainzService.UpdateMusicBrainzArtistAsync(artistId, callback =>
                    {
                        if (callback.Status == UpdateMBStatus.Updating)
                        {
                            AnsiConsole.WriteLine(Markup.Escape($"Importing Album '{callback.CurrentAlbum?.Title}', Artist '{callback.Artist?.Name}'"));
                            ctx.Status(Markup.Escape($"Updating Music Brainz Artist '{callback.Artist?.Name}' Albums {callback.Progress} of {callback.Albums?.Count}"));
                        }
                        else if(callback.Status == UpdateMBStatus.SkippedSyncedWithin)
                        {
                            AnsiConsole.WriteLine(Markup.Escape($"Skipped synchronizing for MusicBrainz ArtistId '{callback?.ArtistId}' synced already within {_musicBrainzService.PreventUpdateWithinDays}days"));
                        }
                    });
                }
            });
    }
    
    public async Task UpdateAllMusicBrainzArtistsAsync()
    {
        var artistIds = await _musicBrainzArtistRepository.GetAllArtistIdsAsync();
        
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(Markup.Escape($"Updating all Music Brainz Artist..."), async ctx =>
            {
                foreach (var artistId in artistIds)
                {
                    await _musicBrainzService.UpdateMusicBrainzArtistAsync(artistId, callback =>
                    {
                        if (callback.Status == UpdateMBStatus.Updating)
                        {
                            AnsiConsole.WriteLine(Markup.Escape($"Importing Album '{callback.CurrentAlbum?.Title}', Artist '{callback.Artist?.Name}'"));
                            ctx.Status(Markup.Escape($"Updating MusicBrainz Artist '{callback.Artist?.Name}' Albums {callback.Progress} of {callback.Albums?.Count}"));
                        }
                        else if(callback.Status == UpdateMBStatus.SkippedSyncedWithin)
                        {
                            AnsiConsole.WriteLine(Markup.Escape($"Skipped synchronizing for MusicBrainz ArtistId '{callback?.ArtistId}' synced already within {_musicBrainzService.PreventUpdateWithinDays}days"));
                        }
                    });
                }
            });
    }
}