using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using MiniMediaScanner.Models;
using SmartFormat;

namespace MiniMediaScanner.Commands;

[Command("normalizefile", 
    Description = @"Normalize/Standardize all your media file names to a common standard. 
Every word gets capatalized (rest of the letters lowercase) except roman letters, all uppercase.
Small words are lowercase: of, the, and, in, on, at, for, to, a
Special characters are replaced: – to -, — to -, … to ...
Seperators between words are kept: : - _ / ,")]
public class NormalizeFileCommand : ICommand
{
    [CommandOption("connection-string", 
        'C', 
        Description = "ConnectionString for Postgres database.", 
        EnvironmentVariable = "CONNECTIONSTRING",
        IsRequired = true)]
    public required string ConnectionString { get; init; }
    
    [CommandOption("artist", 'a', Description = "Artistname", IsRequired = false)]
    public string Artist { get; set; }
    
    [CommandOption("album", 'b', Description = "target Album", IsRequired = false)]
    public string Album { get; set; }
    
    [CommandOption("normalize-artist-name", 'A', Description = "normalize Artistname", IsRequired = false)]
    public bool NormalizeArtistName { get; set; }
    
    [CommandOption("normalize-album-name", 'B', Description = "normalize Albumname", IsRequired = false)]
    public bool NormalizeAlbumName { get; set; }
    
    [CommandOption("normalize-title-name", 'T', Description = "normalize music Title", IsRequired = false)]
    public bool NormalizeTitleName { get; set; }
    
    [CommandOption("overwrite", 'w', Description = "overwrite existing files.", IsRequired = false)]
    public bool Overwrite { get; set; }

    [CommandOption("sub-directory-depth", 's', Description = "sub-directory depth to root-folder.", IsRequired = false)]
    public int SubDirectoryDepth { get; set; } = 0;

    [CommandOption("rename", 'r', Description = "rename file.", IsRequired = false)]
    public bool Rename { get; set; } = false;

    [CommandOption("file-format", 'f', Description = "rename file format (required for renaming) {MetadataId} {Path} {Title} {AlbumId} {ArtistName} {AlbumName} {Tag_AllJsonTags} {Tag_Track} {Tag_TrackCount} {Tag_Disc} {Tag_DiscCount}.", IsRequired = false)]
    public string FileFormat { get; set; } = string.Empty;

    [CommandOption("directory-format", 'D', Description = "rename directory format (required for renaming) {MetadataId} {Path} {Title} {AlbumId} {ArtistName} {AlbumName} {Tag_AllJsonTags} {Tag_Track} {Tag_TrackCount} {Tag_Disc} {Tag_DiscCount}.", IsRequired = false)]
    public string DirectoryFormat { get; set; } = string.Empty;

    [CommandOption("directory-seperator", 'S', Description = "Directory Seperator replacer, replace '/' '\\' to .e.g. '_'.", IsRequired = false)]
    public string DirectorySeperator { get; set; } = "_";
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (!NormalizeArtistName && !NormalizeAlbumName && !NormalizeTitleName && !Rename)
        {
            Console.WriteLine("Nothing todo... NormalizeArtistName, NormalizeAlbumName, NormalizeTitleName, Rename options are all false.");
            return;
        }
        
        var handler = new NormalizeFileCommandHandler(ConnectionString);

        if (Rename && string.IsNullOrWhiteSpace(FileFormat))
        {
            Console.WriteLine("File Format is required.");
            return;
        }
        if (Rename && string.IsNullOrWhiteSpace(DirectoryFormat))
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
                NormalizeArtistName, 
                NormalizeAlbumName, 
                NormalizeTitleName, 
                Overwrite, 
                SubDirectoryDepth, 
                Rename, 
                FileFormat, 
                DirectoryFormat, 
                DirectorySeperator);
        }
        else
        {
            await handler.NormalizeFilesAsync(Artist, 
                Album, 
                NormalizeArtistName, 
                NormalizeAlbumName, 
                NormalizeTitleName, 
                Overwrite, 
                SubDirectoryDepth, 
                Rename, 
                FileFormat, 
                DirectoryFormat, 
                DirectorySeperator);
        }
        
    }
}