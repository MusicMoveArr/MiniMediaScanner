using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("missing", Description = "Check for missing music")]
public class MissingCommand : ICommand
{
    [CommandOption("connection-string", 
        'C', 
        Description = "ConnectionString for Postgres database.", 
        EnvironmentVariable = "CONNECTIONSTRING",
        IsRequired = true)]
    public required string ConnectionString { get; init; }
    
    [CommandOption("artist", 'a', Description = "Artistname", IsRequired = false)]
    public string Artist { get; set; }

    [CommandOption("provider", 'p', Description = "Provider can be either MusicBrainz or Spotify.", IsRequired = false)]
    public string Provider { get; set; } = "musicbrainz";
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (string.IsNullOrWhiteSpace(Provider) ||
            (Provider.ToLower() != "musicbrainz" && Provider.ToLower() != "spotify"))
        {
            Console.WriteLine("Provider must be either 'musicbrainz' or 'spotify'");
            return;
        }
        var handler = new MissingCommandHandler(ConnectionString);

        if (string.IsNullOrWhiteSpace(Artist))
        {
            await handler.CheckAllMissingTracksAsync(Provider);
        }
        else
        {
            await handler.CheckMissingTracksByArtistAsync(Artist, Provider);
        }
    }
}