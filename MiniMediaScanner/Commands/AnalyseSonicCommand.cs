using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("analysesonic", Description = "Analyse Music Characteristics like Mood (Happy, Sad, Relaxed etc), Timbre, Dance Ability, Approach Ability, Genre's and other data about your music using discogs-effnet machine learning models")]
public class AnalyseSonicCommand : ICommand
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
        EnvironmentVariable = "ANALYSESONIC_ARTIST")]
    public string Artist { get; set; }
    
    [CommandOption("album", 'b',
        Description = "target Album", 
        IsRequired = false,
        EnvironmentVariable = "ANALYSESONIC_ALBUM")]
    public string Album { get; set; }

    [CommandOption("threads", 
        Description = "Amount of threads to use.", 
        IsRequired = false,
        EnvironmentVariable = "ANALYSESONIC_THREADS")]
    public int Threads { get; set; } = 4;
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new AnalyseSonicCommandHandler(ConnectionString, Threads);

        if (string.IsNullOrWhiteSpace(Artist))
        {
            await handler.CheckAllTracksAsync(Album);
        }
        else
        {
            await handler.CheckAllTracksAsync(Artist, Album);
        }
        
    }
}