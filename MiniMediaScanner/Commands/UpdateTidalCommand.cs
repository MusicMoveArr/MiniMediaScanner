using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

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
    public required string TidalClientId { get; init; }
    
    [CommandOption("tidal-secret-id", 's', 
        Description = "Tidal Secret Id, to use for the Tidal API.", 
        IsRequired = true,
        EnvironmentVariable = "UPDATETIDAL_TIDAL_SECRET_ID")]
    public required string TidalSecretId { get; init; }

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
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new UpdateTidalCommandHandler(ConnectionString, TidalClientId, TidalSecretId, TidalCountryCode, ProxyFile, Proxy, ProxyMode);

        if (!string.IsNullOrWhiteSpace(Artist))
        {
            await handler.UpdateTidalArtistsByNameAsync(Artist);
        }
        else
        {
            await handler.UpdateAllTidalArtistsAsync();
        }
        
    }
}