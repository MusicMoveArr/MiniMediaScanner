using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("updatespotify", Description = "Update Spotify metadata")]
public class UpdateSpotifyCommand : ICommand
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
        EnvironmentVariable = "UPDATESPOTIFY_ARTIST")]
    public string Artist { get; set; }
    
    [CommandOption("spotify-client-id", 'c', 
        Description = "Spotify Client Id, to use for the Spotify API.", 
        IsRequired = true,
        EnvironmentVariable = "UPDATESPOTIFY_SPOTIFY_CLIENT_ID")]
    public required string SpotifyClientId { get; init; }
    
    [CommandOption("spotify-secret-id", 's', 
        Description = "Spotify Secret Id, to use for the Spotify API.", 
        IsRequired = true,
        EnvironmentVariable = "UPDATESPOTIFY_SPOTIFY_SECRET_ID")]
    public required string SpotifySecretId { get; init; }

    [CommandOption("api-delay", 'D', 
        Description = "Api Delay in seconds after each API call to prevent rate limiting.", 
        IsRequired = false,
        EnvironmentVariable = "UPDATESPOTIFY_API_DELAY")]
    public int ApiDelay { get; set; } = 10;
    
    [CommandOption("prevent-update-within-days",
        Description = "Prevent updating existing artists within x days from the last pull/update",
        IsRequired = false,
        EnvironmentVariable = "UPDATESPOTIFY_PREVENT_UPDATE_WITHIN_DAYS")]
    public int PreventUpdateWithinDays { get; set; } = 7;
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new UpdateSpotifyCommandHandler(ConnectionString, SpotifyClientId, SpotifySecretId, ApiDelay, PreventUpdateWithinDays);

        if (!string.IsNullOrWhiteSpace(Artist))
        {
            await handler.UpdateSpotifyArtistsByNameAsync(Artist);
        }
        else
        {
            await handler.UpdateAllSpotifyArtistsAsync();
        }
    }
}