using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("coverartspotify", Description = "Download Cover art from Spotify for Artist and Album")]
public class CoverArtSpotifyCommand : ICommand
{
    [CommandOption("connection-string", 
        'C', 
        Description = "ConnectionString for Postgres database.", 
        EnvironmentVariable = "CONNECTIONSTRING",
        IsRequired = true)]
    public required string ConnectionString { get; init; }
    
    [CommandOption("artist", 'a', Description = "Artistname", IsRequired = false)]
    public string Artist { get; set; }
    
    [CommandOption("album", 'b', Description = "target Album", IsRequired = false)]
    public string Album { get; set; }
    
    [CommandOption("album-filename", 'f', Description = "Filename e.g. cover.jpg.", IsRequired = false)]
    public string AlbumFilename { get; set; } = "cover.jpg";

    [CommandOption("artist-filename", 'g', Description = "Filename e.g. cover.jpg.", IsRequired = false)]
    public string ArtistFilename { get; set; } = "cover.jpg";
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new CoverArtSpotifyCommandHandler(ConnectionString);

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