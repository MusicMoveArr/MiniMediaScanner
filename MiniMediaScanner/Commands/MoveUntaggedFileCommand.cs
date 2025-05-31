using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using MiniMediaScanner.Models;
using SmartFormat;

namespace MiniMediaScanner.Commands;

[Command("moveuntaggedfile", 
    Description = @"Move untagged files to another directory")]
public class MoveUntaggedCommand : ICommand
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
        EnvironmentVariable = "MOVEUNTAGGEDFILES_ARTIST")]
    public string Artist { get; set; }
    
    [CommandOption("album", 'b', 
        Description = "target Album", 
        IsRequired = false,
        EnvironmentVariable = "MOVEUNTAGGEDFILES_ALBUM")]
    public string Album { get; set; }

    [CommandOption("target-folder", 'T', 
        Description = "Move the untagged files to this target folder.", 
        IsRequired = true,
        EnvironmentVariable = "MOVEUNTAGGEDFILES_TARGET_FOLDER")]
    public string TargetFolder { get; set; } = string.Empty;
    
    [CommandOption("overwrite", 'w', 
        Description = "overwrite existing files.", 
        IsRequired = false,
        EnvironmentVariable = "MOVEUNTAGGEDFILES_OVERWRITE")]
    public bool Overwrite { get; set; }

    [CommandOption("file-format", 'f', 
        Description = "file format {MetadataId} {Path} {Title} {AlbumId} {ArtistName} {AlbumName} {Tag_AllJsonTags} {Tag_Track} {Tag_TrackCount} {Tag_Disc} {Tag_DiscCount}.", 
        IsRequired = true,
        EnvironmentVariable = "MOVEUNTAGGEDFILES_FILE_FORMAT")]
    public string FileFormat { get; set; } = string.Empty;

    [CommandOption("directory-format", 'D', 
        Description = "directory format {MetadataId} {Path} {Title} {AlbumId} {ArtistName} {AlbumName} {Tag_AllJsonTags} {Tag_Track} {Tag_TrackCount} {Tag_Disc} {Tag_DiscCount}.", 
        IsRequired = true,
        EnvironmentVariable = "MOVEUNTAGGEDFILES_DIRECTORY_FORMAT")]
    public string DirectoryFormat { get; set; } = string.Empty;

    [CommandOption("directory-seperator", 'S', 
        Description = "Directory Seperator replacer, replace '/' '\\' to .e.g. '_'.", 
        IsRequired = false,
        EnvironmentVariable = "MOVEUNTAGGEDFILES_DIRECTORY_SEPARATOR")]
    public string DirectorySeperator { get; set; } = "_";
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new MoveUntaggedCommandHandler(ConnectionString);

        if (string.IsNullOrWhiteSpace(FileFormat))
        {
            Console.WriteLine("File Format is required.");
            return;
        }
        if (string.IsNullOrWhiteSpace(DirectoryFormat))
        {
            Console.WriteLine("Directory Format is required.");
            return;
        }

        if (!new DirectoryInfo(TargetFolder).Exists)
        {
            Console.WriteLine($"Target Directory does not exist '{TargetFolder}'.");
            return;
        }
        
        MetadataModel file = new MetadataModel();
        file.ArtistName = "Mini";
        file.AlbumName = "Media";
        file.Title = "Mini Media";
        file.Tag_Disc = 1;
        file.Tag_Track = 7;
        
        //run small test to see if format is correct
        string newFileName = handler.GetFormatName(file, FileFormat, DirectorySeperator);
        
        if (newFileName.Contains("{") || newFileName.Contains("}"))
        {
            Console.WriteLine("File Format is invalid.");
            return;
        }
        
        //run small test to see if format is correct
        string newDirectoryName = handler.GetFormatName(file, DirectoryFormat, DirectorySeperator);
        
        if (newDirectoryName.Contains("{") || newDirectoryName.Contains("}"))
        {
            Console.WriteLine("Directory Format is invalid.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Artist))
        {
            await handler.MoveUntaggedFilesAsync(Album,
                Overwrite, 
                TargetFolder,
                FileFormat, 
                DirectoryFormat, 
                DirectorySeperator);
        }
        else
        {
            await handler.MoveUntaggedFilesAsync(Artist, 
                Album, 
                Overwrite, 
                TargetFolder,
                FileFormat, 
                DirectoryFormat, 
                DirectorySeperator);
        }
        
    }
}