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
    
    [CommandOption("acoustfingerprint-accuracy", 
        Description = "Acoust Fingerprint matching accuracy, 99% is recommended, 98% and lower can mismatch real fast, think of remixes etc",
        IsRequired = false,
        EnvironmentVariable = "DEDUPLICATE_ACOUSTFINGERPRINT_ACCURACY")]
    public int AcoustFingerprintAccuracy { get; set; } = 99;

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
        Description = "Check for duplicate files per album (works better for multi-drive / MergerFS Setups) with a accuracy of X % given with --accuracy or environment variable DEDUPLICATE_ACCURACY",
        IsRequired = false,
        EnvironmentVariable = "DEDUPLICATE_CHECK_EXTENSIONS")]
    public bool CheckAlbumDuplicates { get; set; } = false;
    
    [CommandOption("check-album-extensions", 
        Description = "Similar to --check-extensions with the difference of better support for multi-drive / MergerFS Setups, --check-extensions checks the full path, this option checks per album",
        IsRequired = false,
        EnvironmentVariable = "DEDUPLICATE_CHECK_ALBUM_EXTENSIONS")]
    public bool CheckAlbumExtensions { get; set; } = false;
    
    [CommandOption("check-album-extensions-acoustfingerprint", 
        Description = "Similar to --check-extensions with the difference of better support for multi-drive / MergerFS Setups, --check-extensions checks the full path, this option checks per album",
        IsRequired = false,
        EnvironmentVariable = "DEDUPLICATE_CHECK_ALBUM_EXTENSIONS_ACOUSTFINGERPRINT")]
    public bool CheckAlbumExtensionsAcoustFingerprint { get; set; } = false;
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
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
        if (AcoustFingerprintAccuracy > 100)
        {
            Console.WriteLine("Maximum acoust fingerprint accuracy is 100%");
            return;
        }
        if (AcoustFingerprintAccuracy <= 80)
        {
            Console.WriteLine("80% or lower acoust fingerprint accuracy is not recommended...");
            return;
        }

        var handler = new DeDuplicateFileCommandHandler(ConnectionString);
        handler.Delete = Delete;
        handler.Accuracy = Accuracy;
        handler.Extensions = Extensions;
        handler.CheckExtensions = CheckExtensions;
        handler.CheckVersions = CheckVersions;
        handler.CheckAlbumDuplicates = CheckAlbumDuplicates;
        handler.CheckAlbumExtensions = CheckAlbumExtensions;
        handler.AcoustFingerprintAccuracy = Math.Round(
            Math.Max(AcoustFingerprintAccuracy / 100D, 
                AcoustFingerprintAccuracy == 100 ? 1 : AcoustFingerprintAccuracy / 100D), 
            2, MidpointRounding.AwayFromZero);

        handler.CheckAlbumExtensionsAcoustFingerprint = CheckAlbumExtensionsAcoustFingerprint;
        
        if (string.IsNullOrWhiteSpace(Artist))
        {
            await handler.CheckDuplicateFilesAsync();
        }
        else
        {
            await handler.CheckDuplicateFilesAsync(Artist);
        }
    }
}