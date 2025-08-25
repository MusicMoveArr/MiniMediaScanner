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

    [CommandOption("extensions", 'e',
        Description = "Extensions to keep, in order, first found extension is kept (extensions must be without '.')",
        IsRequired = false,
        EnvironmentVariable = "DEDUPLICATE_EXTENSIONS")]
    public List<string> Extensions { get; set; } = ImportCommandHandler.MediaFileExtensions.ToList();

    [CommandOption("check-extensions", 
        Description = "Check for duplicate filenames with difference file extensions",
        IsRequired = false,
        EnvironmentVariable = "DEDUPLICATE_CHECK_EXTENSIONS")]
    public bool CheckExtensions { get; set; } = false;
    
    [CommandOption("check-versions", 
        Description = "Check for duplicate filename versions, files ending with (1), (2), (3) and so on",
        IsRequired = false,
        EnvironmentVariable = "DEDUPLICATE_CHECK_VERSIONS")]
    public bool CheckVersions { get; set; } = false;
    
    [CommandOption("check-album-duplicates", 
        Description = "Check for duplicate files per album with a accuracy of X % given with --accuracy or environment variable DEDUPLICATE_ACCURACY",
        IsRequired = false,
        EnvironmentVariable = "DEDUPLICATE_CHECK_EXTENSIONS")]
    public bool CheckAlbumDuplicates { get; set; } = false;
    
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
            await handler.CheckDuplicateFilesAsync(Delete, Accuracy, Extensions, CheckExtensions, CheckVersions, CheckAlbumDuplicates);
        }
        else
        {
            await handler.CheckDuplicateFilesAsync(Artist, Delete, Accuracy, Extensions, CheckExtensions, CheckVersions, CheckAlbumDuplicates);
        }
    }
}