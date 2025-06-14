using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("updatedeezer", Description = "Update Deezer metadata")]
public class UpdateDeezerCommand : ICommand
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
        EnvironmentVariable = "UPDATEDEEZER_ARTIST")]
    public string Artist { get; set; }
    
    
    [CommandOption("proxy-file", 
        Description = "HTTP/HTTPS Proxy/Proxies to use to access Deezer.", 
        IsRequired = false,
        EnvironmentVariable = "PROXY_FILE")]
    public string ProxyFile { get; set; }
    
    [CommandOption("proxy", 
        Description = "HTTP/HTTPS Proxy to use to access Deezer.", 
        IsRequired = false,
        EnvironmentVariable = "PROXY_FILE")]
    public string Proxy { get; set; }

    [CommandOption("proxy-mode",
        Description = "Proxy Mode: Random, RoundRobin, StickyTillError, RotateTime, PerArtist.",
        IsRequired = false,
        EnvironmentVariable = "PROXY_MODE")]
    public string ProxyMode { get; set; } = "StickyTillError";
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new UpdateDeezerCommandHandler(ConnectionString, ProxyFile, Proxy, ProxyMode);

        if (!string.IsNullOrWhiteSpace(Artist))
        {
            await handler.UpdateDeezerArtistsByNameAsync(Artist);
        }
        else
        {
            await handler.UpdateAllDeezerArtistsAsync();
        }
        
    }
}