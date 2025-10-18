using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using MiniMediaScanner.Models.Spotify;

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
    public required List<string> SpotifyClientIds { get; init; }
    
    [CommandOption("spotify-secret-id", 's', 
        Description = "Spotify Secret Id, to use for the Spotify API.", 
        IsRequired = true,
        EnvironmentVariable = "UPDATESPOTIFY_SPOTIFY_SECRET_ID")]
    public required List<string> SpotifySecretIds { get; init; }

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
    
    [CommandOption("artist-file",
        Description = "Read from a file line by line to import the artist names",
        IsRequired = false,
        EnvironmentVariable = "UPDATESPOTIFY_ARTIST_FILE")]
    public string ArtistFilePath { get; set; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (SpotifyClientIds.Count != SpotifySecretIds.Count)
        {
            Console.WriteLine("Spotify Id/Secret amount must match");
            return;
        }

        List<SpotifyTokenClientSecret> secretTokens = new List<SpotifyTokenClientSecret>();
        for (int i = 0; i < SpotifyClientIds.Count; i++)
        {
            secretTokens.Add(new SpotifyTokenClientSecret(SpotifyClientIds[i], SpotifySecretIds[i]));
        }
        
        var handler = new UpdateSpotifyCommandHandler(ConnectionString, secretTokens, ApiDelay, PreventUpdateWithinDays);

        if (!string.IsNullOrWhiteSpace(ArtistFilePath) && File.Exists(ArtistFilePath))
        {
            string[] artistNames = File.ReadAllLines(ArtistFilePath);
            int process = 0;
            foreach (var artistName in artistNames)
            {
                Console.WriteLine($"Processing from reading the file: '{artistName}', {process} / {artistNames.Length}");
                await handler.UpdateSpotifyArtistsByNameAsync(artistName);
                process++;
            }
        }
        else
        {
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
}