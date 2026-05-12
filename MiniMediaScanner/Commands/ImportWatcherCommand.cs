using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using MiniMediaScanner.Helpers;

namespace MiniMediaScanner.Commands;

[Command("importwatcher", Description = "Import music to your database automatically from watching directory(ies)")]
public class ImportWatcherCommand : ICommand
{
    [CommandOption("connection-string", 
        'C', 
        Description = "ConnectionString for Postgres database.", 
        EnvironmentVariable = "CONNECTIONSTRING",
        IsRequired = true)]
    public required string ConnectionString { get; init; }
    
    [CommandOption("path", 'p', 
        Description = "From the directory.",
        EnvironmentVariable = "IMPORTWATCHER_PATHS",
        IsRequired = true)]
    public required List<string> Paths { get; init; }

    [CommandOption("update-mb", 'M',
        Description = "Update MusicBrainz.",
        EnvironmentVariable = "IMPORTWATCHER_UPDATE_MB")]
    public bool UpdateMb { get; set; } = true;

    [CommandOption("force", 'f',
        Description = "Force import even if files did not change on disk.",
        EnvironmentVariable = "IMPORTWATCHER_FORCE")]
    public bool Force { get; set; } = false;
    
    [CommandOption("prevent-update-within-days",
        Description = "Prevent updating existing artists within x days from the last pull/update",
        IsRequired = false,
        EnvironmentVariable = "IMPORTWATCHER_PREVENT_UPDATE_WITHIN_DAYS")]
    public int PreventUpdateWithinDays { get; set; } = 7;
    
    [CommandOption("split-artists",
        Description = "Split artists based on external artist id's from MusicBrainz, Deezer etc. This prevents merging 2 different artists together.",
        IsRequired = false,
        EnvironmentVariable = "IMPORTWATCHER_SPLIT_ARTISTS")]
    public bool SplitArtists { get; set; } = false;
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new ImportWatcherCommandHandler(ConnectionString, PreventUpdateWithinDays, Force, UpdateMb, SplitArtists);
        foreach (var path in Paths)
        {
            handler.WatchDirectory(path);
        }

        while (true)
        {
            Thread.Sleep(TimeSpan.FromSeconds(1));
        }
    }
}