using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("missing", Description = "Check for missing music")]
public class MissingCommand : ICommand
{
    private readonly string[] providers = new string[]
    {
        "musicbrainz",
        "spotify",
        "tidal"
    };

[CommandOption("connection-string", 
        'C', 
        Description = "ConnectionString for Postgres database.", 
        EnvironmentVariable = "CONNECTIONSTRING",
        IsRequired = true)]
    public required string ConnectionString { get; init; }
    
    [CommandOption("artist", 'a', 
        Description = "Artistname", 
        IsRequired = false,
        EnvironmentVariable = "MISSING_ARTIST")]
    public string Artist { get; set; }

    [CommandOption("provider", 'p', 
        Description = "Provider can be either MusicBrainz or Spotify.", 
        IsRequired = false,
        EnvironmentVariable = "MISSING_PROVIDER")]
    public string Provider { get; set; } = "musicbrainz";
    
    
    [CommandOption("output", 'o', 
        Description = "Output format, tags available: {Artist} {Album} {Track} {ArtistUrl} {AlbumUrl} {TrackUrl}.", 
        IsRequired = false,
        EnvironmentVariable = "MISSING_OUTPUT")]
    public string Output { get; set; } = "{Artist} - {Album} - {Track}";
    
    [CommandOption("filterout", 'F', 
        Description = "Filterout names from the output.", 
        IsRequired = false,
        EnvironmentVariable = "MISSING_FILTEROUT")]
    public List<string>? FilterOut { get; set; } = new List<string>();
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (string.IsNullOrWhiteSpace(Provider) ||
            !providers.Any(p => string.Equals(p, Provider, StringComparison.CurrentCultureIgnoreCase)))
        {
            Console.WriteLine("Provider must be either 'musicbrainz', 'spotify' or 'tidal'.");
            return;
        }
        var handler = new MissingCommandHandler(ConnectionString);

        if (string.IsNullOrWhiteSpace(Artist))
        {
            await handler.CheckAllMissingTracksAsync(Provider, Output, FilterOut);
        }
        else
        {
            await handler.CheckMissingTracksByArtistAsync(Artist, Provider, Output, FilterOut);
        }
    }
}