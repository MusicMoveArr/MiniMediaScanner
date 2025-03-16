using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("stats", Description = "Get statistics about your media in the database")]
public class StatsCommand : ICommand
{
    [CommandOption("connection-string", 
        'C', 
        Description = "ConnectionString for Postgres database.", 
        EnvironmentVariable = "CONNECTIONSTRING",
        IsRequired = true)]
    public required string ConnectionString { get; init; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new StatsCommandHandler(ConnectionString);

        await handler.ShowStatsAsync();
    }
}