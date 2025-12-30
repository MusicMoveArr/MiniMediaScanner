using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("acoustidcheck", Description = "Check your submissions statuses from AcoustId")]
public class AcoustIdCheckCommand : ICommand
{
    [CommandOption("connection-string", 
        'C', 
        Description = "ConnectionString for Postgres database.", 
        EnvironmentVariable = "CONNECTIONSTRING",
        IsRequired = true)]
    public required string ConnectionString { get; init; }
    
    [CommandOption("acoustid-clientkey", 
        Description = "AcoustId ClientKey for authentication", 
        IsRequired = true,
        EnvironmentVariable = "ACOUSTIDCHECK_CLIENTKEY")]
    public required string AcoustidClientKey { get; init; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new AcoustIdCheckCommandHandler(ConnectionString);
        await handler.SendSubmissionsAsync(AcoustidClientKey);
    }
}