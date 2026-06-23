using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using MiniMediaScanner.Helpers;

namespace MiniMediaScanner.Commands;

[Command("updatelastfm", Description = "Update LastFm metadata")]
public class UpdateLastFmCommand : ICommand
{
    [CommandOption("connection-string", 
        'C', 
        Description = "ConnectionString for Postgres database.", 
        EnvironmentVariable = "CONNECTIONSTRING",
        IsRequired = true)]
    public required string ConnectionString { get; init; }
    
    [CommandOption("artist", 'a', 
        Description = "Artist filter to update.", 
        IsRequired = false,
        EnvironmentVariable = "UPDATELASTFM_ARTIST")]
    public string Artist { get; set; }
    
    [CommandOption("lastfm-apikey", 
        Description = "Last.Fm ApiKey, to use for the Last.Fm API.", 
        IsRequired = true,
        EnvironmentVariable = "UPDATELASTFM_LASTFM_APIKEY")]
    public required string LastFmApiKey { get; init; }
    
    [CommandOption("lastfm-shared-secret", 
        Description = "Last.Fm Shared Secret, to use for the Last.Fm API.", 
        IsRequired = true,
        EnvironmentVariable = "UPDATELASTFM_LASTFM_SHARED_SECRET")]
    public required string LastFmSharedSecret { get; init; }

    [CommandOption("prevent-update-within-days",
        Description = "Prevent updating existing artists within x days from the last pull/update",
        IsRequired = false,
        EnvironmentVariable = "UPDATELASTFM_PREVENT_UPDATE_WITHIN_DAYS")]
    public int PreventUpdateWithinDays { get; set; } = 7;

    [CommandOption("artist-file",
        Description = "Read from a file line by line to import the artist names",
        IsRequired = false,
        EnvironmentVariable = "UPDATELASTFM_ARTIST_FILE")]
    public string ArtistFilePath { get; set; }
    
    [CommandOption("update-nonpulled-artists",
        Description = "Update artists that have not been pulled fully before, first",
        IsRequired = false,
        EnvironmentVariable = "UPDATELASTFM_NONPULLED_ARTISTS")]
    public bool UpdateNonpulledArtists { get; set; }

    [CommandOption("max-album-count",
        Description = "Maximum amount of albums to pull per artist",
        IsRequired = false,
        EnvironmentVariable = "UPDATELASTFM_MAX_ALBUM_COUNT")]
    public int MaxAlbumCountToPull { get; set; } = 1000;
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new UpdateLastFmCommandHandler(
            ConnectionString, 
            LastFmApiKey,
            LastFmSharedSecret,
            PreventUpdateWithinDays,
            MaxAlbumCountToPull);

        if (!string.IsNullOrWhiteSpace(ArtistFilePath) && File.Exists(ArtistFilePath))
        {
            int process = 0;
            await PagedReadLineHelper.ReadLinesAsync(ArtistFilePath, async (artistName, readLineCount) =>
            {
                Console.WriteLine($"Processing from reading the file: '{artistName}', {process} / {readLineCount}");
                await handler.UpdateLastFmArtistsByNameAsync(artistName);
                process++;
            });
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(Artist))
            {
                await handler.UpdateLastFmArtistsByNameAsync(Artist);
            }
            else
            {
                await handler.UpdateAllLastFmArtistsAsync(UpdateNonpulledArtists);
            }
        }
    }
}