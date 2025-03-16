using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("removetag", Description = "Remove tags")]
public class RemoveTagCommand : ICommand
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
    
    [CommandOption("tag", 't', Description = "The tag to remove from media.", IsRequired = false)]
    public string Tag { get; set; }
    
    [CommandOption("tags", 'T', Description = "The tags to remove from media.", IsRequired = false)]
    public List<string> Tags { get; set; }

    [CommandOption("confirm", 'y', Description = "Always confirm automatically.", IsRequired = false)]
    public bool Confirm { get; set; } = false;
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new RemoveTagCommandHandler(ConnectionString);
        if (Tags == null)
        {
            Tags = new List<string>();
        }

        if (!string.IsNullOrWhiteSpace(Tag))
        {
            Tags.Add(Tag);
        }

        if (!Tags.Any())
        {
            Console.WriteLine("No tags were specified.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Artist))
        {
            await handler.RemoveTagFromMediaAsync(Album, Tags, Confirm);
        }
        else
        {
            await handler.RemoveTagFromMediaAsync(Artist, Album, Tags, Confirm);
        }
    }
}