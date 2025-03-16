using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("convert", Description = "Convert media for example FLAC > M4A")]
public class ConvertMediaCommand : ICommand
{
    [CommandOption("connection-string", 
        'C', 
        Description = "ConnectionString for Postgres database.", 
        EnvironmentVariable = "CONNECTIONSTRING",
        IsRequired = true)]
    public required string ConnectionString { get; init; }
    
    [CommandOption("artist", 'a', Description = "Artistname", IsRequired = false)]
    public string Artist { get; set; }
    
    [CommandOption("from-extension", 'f', Description = "From extension.", IsRequired = true)]
    public string FromExtension { get; init; }
    
    [CommandOption("to-extension", 't', Description = "To extension.", IsRequired = true)]
    public string ToExtension { get; init; }
    
    [CommandOption("codec", 'c', Description = "Codec e.g. aac.", IsRequired = true)]
    public string Codec { get; init; }
    
    [CommandOption("bitrate", 'b', Description = "Bitrate e.g. 320k.", IsRequired = true)]
    public string Bitrate { get; init; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new ConvertMediaCommandHandler(ConnectionString);

        if (string.IsNullOrWhiteSpace(Artist))
        {
            await handler.ConvertAllArtistsAsync(FromExtension, ToExtension, Codec, Bitrate);
        }
        else
        {
            await handler.ConvertByArtistAsync(FromExtension, ToExtension, Artist, Codec, Bitrate);
        }
    }
}