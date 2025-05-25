using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using MiniMediaScanner.Models;
using SmartFormat;

namespace MiniMediaScanner.Commands;

[Command("renamefile", 
    Description = @"Rename media files based on format")]
public class RenameFileCommand : ICommand
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
        EnvironmentVariable = "NORMALIZEFILE_ARTIST")]
    public string Artist { get; set; }
    
    [CommandOption("album", 'b', 
        Description = "target Album", 
        IsRequired = false,
        EnvironmentVariable = "NORMALIZEFILE_ALBUM")]
    public string Album { get; set; }
    
    [CommandOption("overwrite", 'w', 
        Description = "overwrite existing files.", 
        IsRequired = false,
        EnvironmentVariable = "NORMALIZEFILE_OVERWRITE")]
    public bool Overwrite { get; set; }

    [CommandOption("sub-directory-depth", 's', 
        Description = "sub-directory depth to root-folder.", 
        IsRequired = false,
        EnvironmentVariable = "NORMALIZEFILE_SUB_DIRECTORY_DEPTH")]
    public int SubDirectoryDepth { get; set; } = 0;

    [CommandOption("file-format", 'f', 
        Description = "rename file format (required for renaming) {MetadataId} {Path} {Title} {AlbumId} {ArtistName} {AlbumName} {Tag_AllJsonTags} {Tag_Track} {Tag_TrackCount} {Tag_Disc} {Tag_DiscCount}.", 
        IsRequired = false,
        EnvironmentVariable = "NORMALIZEFILE_FILE_FORMAT")]
    public string FileFormat { get; set; } = string.Empty;

    [CommandOption("directory-format", 'D', 
        Description = "rename directory format (required for renaming) {MetadataId} {Path} {Title} {AlbumId} {ArtistName} {AlbumName} {Tag_AllJsonTags} {Tag_Track} {Tag_TrackCount} {Tag_Disc} {Tag_DiscCount}.", 
        IsRequired = false,
        EnvironmentVariable = "NORMALIZEFILE_DIRECTORY_FORMAT")]
    public string DirectoryFormat { get; set; } = string.Empty;

    [CommandOption("directory-seperator", 'S', 
        Description = "Directory Seperator replacer, replace '/' '\\' to .e.g. '_'.", 
        IsRequired = false,
        EnvironmentVariable = "NORMALIZEFILE_DIRECTORY_SEPARATOR")]
    public string DirectorySeperator { get; set; } = "_";
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var handler = new RenameFileCommandHandler(ConnectionString);

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
            await handler.NormalizeFilesAsync(Album,
                Overwrite, 
                SubDirectoryDepth, 
                FileFormat, 
                DirectoryFormat, 
                DirectorySeperator);
        }
        else
        {
            await handler.NormalizeFilesAsync(Artist, 
                Album, 
                Overwrite, 
                SubDirectoryDepth, 
                FileFormat, 
                DirectoryFormat, 
                DirectorySeperator);
        }
        
    }
}