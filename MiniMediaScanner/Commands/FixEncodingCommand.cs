using System.Text;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace MiniMediaScanner.Commands;

[Command("fixencoding", Description = "Fix encoding issues by re-encoding filename/tags to the correct encoding")]
public class FixEncodingCommand : ICommand
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
        EnvironmentVariable = "FIXENCODING_ARTIST")]
    public string Artist { get; set; }
    
    [CommandOption("album", 'b', 
        Description = "target Album", 
        IsRequired = false,
        EnvironmentVariable = "FIXENCODING_ALBUM")]
    public string Album { get; set; }

    [CommandOption("from-encoding",
        Description = "From encoding name of codepage number.", 
        IsRequired = true,
        EnvironmentVariable = "FIXENCODING_FROM_ENCODING")]
    public string FromEncoding { get; set; }
    
    [CommandOption("whitelist-regex",
        Description = "Only process tags that go matched through the regex, preventing accidental re-encodings.", 
        IsRequired = true,
        EnvironmentVariable = "FIXENCODING_WHITELIST_REGEX")]
    public string WhitelistRegex { get; set; }

    [CommandOption("target-encoding",
        Description = "Target encoding name of codepage number.", 
        IsRequired = true,
        EnvironmentVariable = "FIXENCODING_TARGET_ENCODING")]
    public string TargetEncoding { get; set; }
    
    [CommandOption("verify-regex",
        Description = "Verify if the re-encoding was done successfully.", 
        IsRequired = true,
        EnvironmentVariable = "FIXENCODING_EXPECTED_REGEX")]
    public string VerifyRegex { get; set; }
    
    [CommandOption("blacklist-regex",
        Description = "Blacklist Regex that will disallow to set the new value if matched with the new value.", 
        IsRequired = true,
        EnvironmentVariable = "FIXENCODING_BLACKLIST_REGEX")]
    public string BlacklistRegex { get; set; }
    
    [CommandOption("confirm", 'y', 
        Description = "Always confirm automatically.", 
        IsRequired = false,
        EnvironmentVariable = "FIXENCODING_CONFIRM")]
    public bool Confirm { get; set; } = false;
    
    [CommandOption("overwrite", 'w', 
        Description = "overwrite existing files.", 
        IsRequired = false,
        EnvironmentVariable = "FIXENCODING_OVERWRITE")]
    public bool Overwrite { get; set; }

    [CommandOption("file-format", 'f', 
        Description = "file format {MetadataId} {Path} {Title} {AlbumId} {ArtistName} {AlbumName} {Tag_AllJsonTags} {Tag_Track} {Tag_TrackCount} {Tag_Disc} {Tag_DiscCount}.", 
        IsRequired = true,
        EnvironmentVariable = "FIXENCODING_FILE_FORMAT")]
    public string FileFormat { get; set; } = string.Empty;

    [CommandOption("directory-format", 'D', 
        Description = "directory format {MetadataId} {Path} {Title} {AlbumId} {ArtistName} {AlbumName} {Tag_AllJsonTags} {Tag_Track} {Tag_TrackCount} {Tag_Disc} {Tag_DiscCount}.", 
        IsRequired = true,
        EnvironmentVariable = "FIXENCODING_DIRECTORY_FORMAT")]
    public string DirectoryFormat { get; set; } = string.Empty;

    [CommandOption("directory-seperator", 'S', 
        Description = "Directory Seperator replacer, replace '/' '\\' to .e.g. '_'.", 
        IsRequired = false,
        EnvironmentVariable = "FIXENCODING_DIRECTORY_SEPARATOR")]
    public string DirectorySeperator { get; set; } = "_";

    [CommandOption("dry-run", 
        Description = "Dry run, no changes will be made", 
        IsRequired = false,
        EnvironmentVariable = "FIXENCODING_DRY_RUN")]
    public bool DryRun { get; set; } = false;
    
    [CommandOption("sub-directory-depth", 's', 
        Description = "sub-directory depth to root-folder.", 
        IsRequired = false,
        EnvironmentVariable = "FIXENCODING_SUB_DIRECTORY_DEPTH")]
    public int SubDirectoryDepth { get; set; } = 0;
    
    [CommandOption("tags", 
        Description = "Tags to re-encode, seperate tagnames using ':'", 
        IsRequired = true,
        EnvironmentVariable = "FIXENCODING_TAGS")]
    public List<string> Tags { get; set; }

    [CommandOption("check-filename",
        Description = "Check as well the filename against regex",
        IsRequired = false,
        EnvironmentVariable = "FIXENCODING_CHECK_FILENAME")]
    public bool CheckFilename { get; set; } = false;
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var handler = new FixEncodingCommandHandler(ConnectionString);
        handler.Album = Album;
        handler.WhitelistRegex = WhitelistRegex;
        handler.VerifyRegex = VerifyRegex;
        handler.BlacklistRegex = BlacklistRegex;
        handler.AutoConfirm = Confirm;
        handler.Overwrite = Overwrite;
        handler.FileFormat = FileFormat;
        handler.DirectoryFormat = DirectoryFormat;
        handler.DirectorySeperator = DirectorySeperator;
        handler.DryRun = DryRun;
        handler.Tags = Tags;
        handler.SubDirectoryDepth = SubDirectoryDepth;
        handler.CheckFilename = CheckFilename;

        handler.FromEncoding = int.TryParse(FromEncoding, out int from)
            ? Encoding.GetEncoding(from)
            : Encoding.GetEncoding(FromEncoding);

        handler.TargetEncoding = int.TryParse(TargetEncoding, out int target)
            ? Encoding.GetEncoding(target)
            : Encoding.GetEncoding(TargetEncoding);

        if (string.IsNullOrWhiteSpace(Artist))
        {
            await handler.FixEncodingAsync();
        }
        else
        {
            await handler.FixEncodingAsync(Artist);
        }
    }
}