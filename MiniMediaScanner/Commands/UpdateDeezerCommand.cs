using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using MiniMediaScanner.Helpers;

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
        EnvironmentVariable = "PROXY")]
    public string Proxy { get; set; }

    [CommandOption("proxy-mode",
        Description = "Proxy Mode: Random, RoundRobin, StickyTillError, RotateTime, PerArtist.",
        IsRequired = false,
        EnvironmentVariable = "PROXY_MODE")]
    public string ProxyMode { get; set; } = "StickyTillError";


    [CommandOption("save-track-token",
        Description = "Save the track_token that is returned by the Deezer API into the database, disabling this saves space in postgres.",
        IsRequired = false,
        EnvironmentVariable = "UPDATEDEEZER_SAVE_TRACK_TOKEN")]
    public bool SaveTrackToken { get; set; } = true;

    [CommandOption("save-preview-url",
        Description = "Save the preview_url that is returned by the Deezer API into the database, disabling this saves space in postgres.",
        IsRequired = false,
        EnvironmentVariable = "UPDATEDEEZER_SAVE_PREVIEW_URL")]
    public bool SavePreviewUrl { get; set; } = true;

    [CommandOption("threads",
        Description = "The amount of threads to use.",
        IsRequired = false,
        EnvironmentVariable = "UPDATEDEEZER_THREADS")]
    public int Threads { get; set; } = 1;
    
    [CommandOption("prevent-update-within-days",
        Description = "Prevent updating existing artists within x days from the last pull/update",
        IsRequired = false,
        EnvironmentVariable = "UPDATEDEEZER_PREVENT_UPDATE_WITHIN_DAYS")]
    public int PreventUpdateWithinDays { get; set; } = 7;
    
    [CommandOption("artist-file",
        Description = "Read from a file line by line to import the artist names",
        IsRequired = false,
        EnvironmentVariable = "UPDATEDEEZER_ARTIST_FILE")]
    public string ArtistFilePath { get; set; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new UpdateDeezerCommandHandler(
            ConnectionString, 
            ProxyFile, 
            Proxy, 
            ProxyMode, 
            SaveTrackToken, 
            SavePreviewUrl, 
            Threads, 
            PreventUpdateWithinDays);

        if (!string.IsNullOrWhiteSpace(ArtistFilePath) && File.Exists(ArtistFilePath))
        {
            int process = 0;
            await PagedReadLineHelper.ReadLinesAsync(ArtistFilePath, async (artistName, readLineCount) =>
            {
                Console.WriteLine($"Processing from reading the file: '{artistName}', {process} / {readLineCount}");
                await handler.UpdateDeezerArtistsByNameAsync(artistName);
                process++;
            });
        }
        else
        {
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
}