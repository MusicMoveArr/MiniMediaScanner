using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using MiniMediaScanner.Models.Tidal;

namespace MiniMediaScanner.Commands;

[Command("updatetidal", Description = "Update Tidal metadata")]
public class UpdateTidalCommand : ICommand
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
        EnvironmentVariable = "UPDATETIDAL_ARTIST")]
    public string Artist { get; set; }
    
    [CommandOption("tidal-client-id", 'c', 
        Description = "Tidal Client Id, to use for the Tidal API.", 
        IsRequired = true,
        EnvironmentVariable = "UPDATETIDAL_TIDAL_CLIENT_ID")]
    public required List<string> TidalClientIds { get; init; }
    
    [CommandOption("tidal-secret-id", 's', 
        Description = "Tidal Secret Id, to use for the Tidal API.", 
        IsRequired = true,
        EnvironmentVariable = "UPDATETIDAL_TIDAL_SECRET_ID")]
    public required List<string> TidalSecretIds { get; init; }

    [CommandOption("country-code", 'G',
        Description = "Tidal's CountryCode (e.g. US, FR, NL, DE etc).",
        EnvironmentVariable = "UPDATETIDAL_COUNTRY_CODE")]
    public string TidalCountryCode { get; set; } = "US";
    
    
    [CommandOption("proxy-file", 
        Description = "HTTP/HTTPS Proxy/Proxies to use to access Deezer.", 
        IsRequired = false,
        EnvironmentVariable = "PROXY_FILE")]
    public string ProxyFile { get; set; }
    
    [CommandOption("proxy", 
        Description = "HTTP/HTTPS Proxy to use to access Deezer.", 
        IsRequired = false,
        EnvironmentVariable = "PROXY")]
    public string Proxy { get; set; }

    [CommandOption("proxy-mode",
        Description = "Proxy Mode: Random, RoundRobin, StickyTillError, RotateTime, PerArtist.",
        IsRequired = false,
        EnvironmentVariable = "PROXY_MODE")]
    public string ProxyMode { get; set; } = "StickyTillError";

    [CommandOption("prevent-update-within-days",
        Description = "Prevent updating existing artists within x days from the last pull/update",
        IsRequired = false,
        EnvironmentVariable = "UPDATETIDAL_PREVENT_UPDATE_WITHIN_DAYS")]
    public int PreventUpdateWithinDays { get; set; } = 7;

    [CommandOption("artist-file",
        Description = "Read from a file line by line to import the artist names",
        IsRequired = false,
        EnvironmentVariable = "UPDATETIDAL_ARTIST_FILE")]
    public string ArtistFilePath { get; set; }
    
    [CommandOption("update-nonpulled-artists",
        Description = "Update artists that have not been pulled fully before, first",
        IsRequired = false,
        EnvironmentVariable = "UPDATETIDAL_NONPULLED_ARTISTS")]
    public bool UpdateNonpulledArtists { get; set; }

    [CommandOption("ignore-artist-album-amount",
        Description = "Ignore artists that have over a certain amount of albums, >500 albums is not normal",
        IsRequired = false,
        EnvironmentVariable = "UPDATETIDAL_IGNORE_ARTIST_ALBUM_AMOUNT")]
    public int IgnoreArtistAlbumAmount { get; set; } = 500;
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (TidalClientIds.Count != TidalSecretIds.Count)
        {
            Console.WriteLine("Tidal Id/Secret amount must match");
            return;
        }

        List<TidalTokenClientSecret> secretTokens = new List<TidalTokenClientSecret>();
        for (int i = 0; i < TidalClientIds.Count; i++)
        {
            secretTokens.Add(new TidalTokenClientSecret(TidalClientIds[i], TidalSecretIds[i]));
        }
        
        var handler = new UpdateTidalCommandHandler(
            ConnectionString, 
            secretTokens, 
            TidalCountryCode, 
            ProxyFile, 
            Proxy, 
            ProxyMode, 
            PreventUpdateWithinDays,
            IgnoreArtistAlbumAmount);

        if (!string.IsNullOrWhiteSpace(ArtistFilePath) && File.Exists(ArtistFilePath))
        {
            string[] artistNames = File.ReadAllLines(ArtistFilePath);
            int process = 0;
            foreach (var artistName in artistNames)
            {
                Console.WriteLine($"Processing from reading the file: '{artistName}', {process} / {artistNames.Length}");
                await handler.UpdateTidalArtistsByNameAsync(artistName);
                process++;
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(Artist))
            {
                await handler.UpdateTidalArtistsByNameAsync(Artist);
            }
            else
            {
                await handler.UpdateAllTidalArtistsAsync(UpdateNonpulledArtists);
            }
        }
    }
}