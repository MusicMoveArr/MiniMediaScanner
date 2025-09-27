using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using MiniMediaScanner.Helpers;

namespace MiniMediaScanner.Commands;

[Command("import", Description = "Import music to your database")]
public class ImportCommand : ICommand
{
    [CommandOption("connection-string", 
        'C', 
        Description = "ConnectionString for Postgres database.", 
        EnvironmentVariable = "CONNECTIONSTRING",
        IsRequired = true)]
    public required string ConnectionString { get; init; }
    
    [CommandOption("path", 'p', 
        Description = "From the directory.",
        EnvironmentVariable = "IMPORT_PATH",
        IsRequired = true)]
    public required string Path { get; init; }

    [CommandOption("update-mb", 'M',
        Description = "Update MusicBrainz.",
        EnvironmentVariable = "IMPORT_UPDATE_MB")]
    public bool UpdateMb { get; set; } = true;

    [CommandOption("force", 'f',
        Description = "Force import even if files did not change on disk.",
        EnvironmentVariable = "IMPORT_FORCE")]
    public bool Force { get; set; } = false;
    
    [CommandOption("prevent-update-within-days",
        Description = "Prevent updating existing artists within x days from the last pull/update",
        IsRequired = false,
        EnvironmentVariable = "IMPORT_PREVENT_UPDATE_WITHIN_DAYS")]
    public int PreventUpdateWithinDays { get; set; } = 7;
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new ImportCommandHandler(ConnectionString, PreventUpdateWithinDays);
        await handler.ProcessDirectoryAsync(Path, Force, UpdateMb);
    }
}