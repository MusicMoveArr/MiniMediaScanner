using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("deduplicate", Description = "Check for duplicated music per album and delete optionally")]
public class DeDuplicateFileCommand : ICommand
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
        EnvironmentVariable = "DEDUPLICATE_ARTIST")]
    public string Artist { get; set; }
    
    [CommandOption("delete", 'd', 
        Description = "Delete duplicate file", 
        IsRequired = false,
        EnvironmentVariable = "DEDUPLICATE_DELETE")]
    public bool Delete { get; set; }

    [CommandOption("accuracy", 'A',
        Description = "Filename matching accuracy, 98% is recommended",
        IsRequired = false,
        EnvironmentVariable = "DEDUPLICATE_ACCURACY")]
    public int Accuracy { get; set; } = 98;
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new DeDuplicateFileCommandHandler(ConnectionString);

        if (Accuracy <= 50)
        {
            Console.WriteLine("50% or lower accuracy is not recommended...");
            return;
        }
        if (Accuracy >= 100)
        {
            Console.WriteLine("Maximum accuracy is 99%");
            return;
        }

        if (string.IsNullOrWhiteSpace(Artist))
        {
            await handler.CheckDuplicateFilesAsync(Delete, Accuracy);
        }
        else
        {
            await handler.CheckDuplicateFilesAsync(Artist, Delete, Accuracy);
        }
    }
}