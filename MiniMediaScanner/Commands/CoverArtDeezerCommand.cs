using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("coverartdeezer", Description = "Download Cover art from Deezer for Artist and Album")]
public class CoverArtDeezerCommand : ICommand
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
        EnvironmentVariable = "COVERARTDEEZER_ARTIST")]
    public string Artist { get; set; }
    
    [CommandOption("album", 'b', 
        Description = "target Album", 
        IsRequired = false,
        EnvironmentVariable = "COVERARTDEEZER_ALBUM")]
    public string Album { get; set; }
    
    [CommandOption("album-filename", 'f', 
        Description = "Filename e.g. cover.jpg.", 
        IsRequired = false,
        EnvironmentVariable = "COVERARTDEEZER_ALBUM_FILENAME")]
    public string AlbumFilename { get; set; } = "cover.jpg";

    [CommandOption("artist-filename", 'g', 
        Description = "Filename e.g. cover.jpg.", 
        IsRequired = false,
        EnvironmentVariable = "COVERARTDEEZER_ARTIST_FILENAME")]
    public string ArtistFilename { get; set; } = "cover.jpg";
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new CoverArtDeezerCommandHandler(ConnectionString);

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