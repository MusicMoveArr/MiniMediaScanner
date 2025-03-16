using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("splitartist", Description = @"Split an artist the best we can based on MusicBrainzArtistId tag, if multiple artists use the same name.
Tags available: MusicBrainzRemoteId, Name, Country, Type, Date")]
public class SplitArtistCommand : ICommand
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
        EnvironmentVariable = "SPLITARTIST_ARTIST")]
    public string Artist { get; set; }
    
    [CommandOption("album", 'b', 
        Description = "target Album", 
        IsRequired = false,
        EnvironmentVariable = "SPLITARTIST_ALBUM")]
    public string Album { get; set; }
    
    [CommandOption("artist-format", 'f', 
        Description = "artist format for splitting the 2 artists apart.", 
        IsRequired = true,
        EnvironmentVariable = "SPLITARTIST_ARTIST_FORMAT")]
    public required string ArtistFormat { get; init; }
    
    [CommandOption("confirm", 'y', 
        Description = "Always confirm automatically.", 
        IsRequired = false,
        EnvironmentVariable = "SPLITARTIST_CONFIRM")]
    public bool Confirm { get; set; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new SplitArtistCommandHandler(ConnectionString);

        await handler.SplitArtistAsync(Artist, ArtistFormat, Confirm);
    }
}