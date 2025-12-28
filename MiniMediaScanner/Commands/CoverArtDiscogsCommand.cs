using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("coverartdiscogs", Description = "Download Cover art from Discogs for Artist and Album")]
public class CoverArtDiscogsCommand : ICommand
{
    [CommandOption("connection-string", 
        'C', 
        Description = "ConnectionString for Postgres database.", 
        EnvironmentVariable = "CONNECTIONSTRING",
        IsRequired = true)]
    public required string ConnectionString { get; init; }
    
    [CommandOption("artist", 'a', 
        Description = "Artistname", 
        IsRequired = false,
        EnvironmentVariable = "COVERARTDISCOGS_ARTIST")]
    public string Artist { get; set; }
    
    [CommandOption("album", 'b', 
        Description = "target Album", 
        IsRequired = false,
        EnvironmentVariable = "COVERARTDISCOGS_ALBUM")]
    public string Album { get; set; }
    
    [CommandOption("album-filename", 'f', 
        Description = "Filename e.g. cover.jpg.", 
        IsRequired = false,
        EnvironmentVariable = "COVERARTDISCOGS_ALBUM_FILENAME")]
    public string AlbumFilename { get; set; } = "cover.jpg";

    [CommandOption("artist-filename", 'g', 
        Description = "Filename e.g. cover.jpg.", 
        IsRequired = false,
        EnvironmentVariable = "COVERARTDISCOGS_ARTIST_FILENAME")]
    public string ArtistFilename { get; set; } = "cover.jpg";

    [CommandOption("discogs-token", 
        Description = "The Discogs token required to get the covers of Discogs", 
        IsRequired = true,
        EnvironmentVariable = "COVERARTDISCOGS_TOKEN")]
    public string DiscogsToken { get; set; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new CoverArtDiscogsCommandHandler(ConnectionString, DiscogsToken);

        if (string.IsNullOrWhiteSpace(Artist))
        {
            await handler.CheckAllMissingCoversAsync(Album, AlbumFilename, ArtistFilename);
        }
        else
        {
            await handler.CheckAllMissingCoversAsync(Artist, Album, AlbumFilename, ArtistFilename);
        }
    }
}