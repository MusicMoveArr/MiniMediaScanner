using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using MiniMediaScanner.Models.Tidal;

namespace MiniMediaScanner.Commands;

[Command("updatesoundcloud", Description = "Update SoundCloud metadata")]
public class UpdateSoundCloudCommand : ICommand
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
        EnvironmentVariable = "UPDATESOUNDCLOUD_ARTIST")]
    public string Artist { get; set; }
    
    [CommandOption("client-id", 'c', 
        Description = "SoundCloud Client Id, to use for the SoundCloud API.", 
        IsRequired = true,
        EnvironmentVariable = "UPDATESOUNDCLOUD_CLIENT_ID")]
    public required string SoundCloudClientId { get; init; }
    
    [CommandOption("prevent-update-within-days",
        Description = "Prevent updating existing artists within x days from the last pull/update",
        IsRequired = false,
        EnvironmentVariable = "UPDATESOUNDCLOUD_PREVENT_UPDATE_WITHIN_DAYS")]
    public int PreventUpdateWithinDays { get; set; } = 7;

    [CommandOption("artist-file",
        Description = "Read from a file line by line to import the artist names",
        IsRequired = false,
        EnvironmentVariable = "UPDATESOUNDCLOUD_ARTIST_FILE")]
    public string ArtistFilePath { get; set; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new UpdateSoundCloudCommandHandler(
            ConnectionString, 
            SoundCloudClientId, 
            PreventUpdateWithinDays);

        if (!string.IsNullOrWhiteSpace(ArtistFilePath) && File.Exists(ArtistFilePath))
        {
            string[] artistNames = File.ReadAllLines(ArtistFilePath);
            int process = 0;
            foreach (var artistName in artistNames)
            {
                Console.WriteLine($"Processing from reading the file: '{artistName}', {process} / {artistNames.Length}");
                await handler.UpdateSoundCloudArtistsByNameAsync(artistName);
                process++;
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(Artist))
            {
                await handler.UpdateSoundCloudArtistsByNameAsync(Artist);
            }
            else
            {
                await handler.UpdateAllSoundCloudArtistsAsync();
            }
        }
    }
}