using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("fixcollections", Description = "Fix collections by adding the missing artist to the Artists tag")]
public class FixCollectionsCommand : ICommand
{
    [CommandOption("connection-string",  'C', 
        Description = "ConnectionString for Postgres database.", 
        EnvironmentVariable = "CONNECTIONSTRING",
        IsRequired = true)]
    public required string ConnectionString { get; init; }
    
    [CommandOption("artist", 'a', 
        Description = "Artistname", 
        IsRequired = false,
        EnvironmentVariable = "FIXCOLLECTIONS_ARTIST")]
    public string Artist { get; set; }
    
    [CommandOption("label", 'l', 
        Description = "Target label to find songs belonging to a collection", 
        IsRequired = false,
        EnvironmentVariable = "FIXCOLLECTIONS_LABEL")]
    public string TargetLabel { get; set; }
    
    [CommandOption("copyright", 'H', 
        Description = "Target copyright to find songs belonging to a collection", 
        IsRequired = false,
        EnvironmentVariable = "FIXCOLLECTIONS_COPYRIGHT")]
    public string TargetCopyright { get; set; }
    
    [CommandOption("albumregex", 'b', 
        Description = "Target album(s) with regex", 
        IsRequired = true,
        EnvironmentVariable = "FIXCOLLECTIONS_ALBUMREGEX")]
    public required string AlbumRegex { get; set; }
    
    [CommandOption("addartist", 'W', 
        Description = "Add the missing artist to the Artists tag", 
        IsRequired = true,
        EnvironmentVariable = "FIXCOLLECTIONS_ADDARTIST")]
    public required string AddArtist { get; set; }

    [CommandOption("confirm", 'y', 
        Description = "Always confirm automatically.", 
        IsRequired = false,
        EnvironmentVariable = "FIXCOLLECTIONS_CONFIRM")]
    public bool Confirm { get; set; } = false;
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new FixCollectionsCommandHandler(ConnectionString);

        await handler.FindMissingArtistsAsync(Artist, TargetLabel, TargetCopyright, AlbumRegex, AddArtist, Confirm);
    }
}