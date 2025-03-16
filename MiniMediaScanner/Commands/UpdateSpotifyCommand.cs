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
    
    [CommandOption("artist", 'a', Description = "Artist filter to update.", IsRequired = false)]
    public string Artist { get; set; }
    
    [CommandOption("spotify-client-id", 'c', Description = "Spotify Client Id, to use for the Spotify API.", IsRequired = false)]
    public string SpotifyClientId { get; set; }
    
    [CommandOption("spotify-secret-id", 's', Description = "Spotify Secret Id, to use for the Spotify API.", IsRequired = false)]
    public string SpotifySecretId { get; set; }

    [CommandOption("api-delay", 'D', Description = "Api Delay in seconds after each API call to prevent rate limiting.", IsRequired = false)]
    public int ApiDelay { get; set; } = 10;
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new UpdateSpotifyCommandHandler(ConnectionString, SpotifyClientId, SpotifySecretId, ApiDelay);

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