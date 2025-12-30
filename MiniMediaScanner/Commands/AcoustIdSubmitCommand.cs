using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("acoustidsubmit", Description = "Send submissions to AcoustId")]
public class AcoustIdSubmitCommand : ICommand
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
        EnvironmentVariable = "ACOUSTIDSUBMISSION_CLIENTKEY")]
    public required string AcoustidClientKey { get; init; }
    
    [CommandOption("acoustid-userkey", 
        Description = "AcoustId UserKey for authentication", 
        IsRequired = true,
        EnvironmentVariable = "ACOUSTIDSUBMISSION_USERKEY")]
    public required string AcoustidUserKey { get; init; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new AcoustIdSubmitCommandHandler(ConnectionString);

        await handler.SendSubmissionsAsync(AcoustidClientKey, AcoustidUserKey);
    }
}