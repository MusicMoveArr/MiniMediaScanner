using ConsoleAppFramework;
using MiniMediaScanner.Models;
using SmartFormat;

namespace MiniMediaScanner.Commands;

public class NormalizeFileCommand
{
    /// <summary>
    /// Normalize/Standardize all your media file names to a common standard
    /// Every word gets capatalized (rest of the letters lowercase) except roman letters, all uppercase
    /// Small words are lowercase: of, the, and, in, on, at, for, to, a
    /// Special characters are replaced: – to -, — to -, … to ...
    /// Seperators between words are kept: : - _ / , 
    /// </summary>
    /// <param name="connectionString">-C, ConnectionString for Postgres database.</param>
    /// <param name="artist">-a, Artistname.</param>
    /// <param name="album">-b, target Album.</param>
    /// <param name="normalizeArtistName">-na, normalize Artistname.</param>
    /// <param name="normalizeAlbumName">-nb, normalize Albumname.</param>
    /// <param name="normalizeTitleName">-nt, normalize music Title.</param>
    /// <param name="rename">-r, rename file.</param>
    /// <param name="overwrite">-w, overwrite existing files.</param>
    /// <param name="subDirectoryDepth">-s, sub-directory depth to root-folder.</param>
    /// <param name="fileFormat">-f, rename file format (required for renaming) {MetadataId} {Path} {Title} {AlbumId} {ArtistName} {AlbumName} {Tag_AllJsonTags} {Tag_Track} {Tag_TrackCount} {Tag_Disc} {Tag_DiscCount}.</param>
    /// <param name="directoryFormat">-df, rename directory format (required for renaming) {MetadataId} {Path} {Title} {AlbumId} {ArtistName} {AlbumName} {Tag_AllJsonTags} {Tag_Track} {Tag_TrackCount} {Tag_Disc} {Tag_DiscCount}.</param>
    /// <param name="directorySeperator">-ds, Directory Seperator replacer, replace '/' '\' to .e.g. '_'.</param>
    [Command("normalizefile")]
    public static void NormalizeFile(
        string connectionString,
        bool normalizeArtistName,
        bool normalizeAlbumName,
        bool normalizeTitleName,
        bool overwrite,
        int subDirectoryDepth = 0,
        bool rename = false, 
        string fileFormat = "", 
        string directoryFormat = "", 
        string directorySeperator = "_",
        string artist = "", 
        string album = "")
    {
        var handler = new NormalizeFileCommandHandler(connectionString);

        if (rename && string.IsNullOrWhiteSpace(fileFormat))
        {
            Console.WriteLine("File Format is required.");
            return;
        }
        if (rename && string.IsNullOrWhiteSpace(directoryFormat))
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
        string newFileName = handler.GetFormatName(file, fileFormat, directorySeperator);
        
        if (newFileName.Contains("{") || newFileName.Contains("}"))
        {
            Console.WriteLine("File Format is invalid.");
            return;
        }
        
        //run small test to see if format is correct
        string newDirectoryName = handler.GetFormatName(file, directoryFormat, directorySeperator);
        
        if (newDirectoryName.Contains("{") || newDirectoryName.Contains("}"))
        {
            Console.WriteLine("Directory Format is invalid.");
            return;
        }

        if (string.IsNullOrWhiteSpace(artist))
        {
            handler.NormalizeFiles(album,
                normalizeArtistName, 
                normalizeAlbumName, 
                normalizeTitleName, 
                overwrite, 
                subDirectoryDepth, 
                rename, 
                fileFormat, 
                directoryFormat, 
                directorySeperator);
        }
        else
        {
            handler.NormalizeFiles(artist, 
                album, 
                normalizeArtistName, 
                normalizeAlbumName, 
                normalizeTitleName, 
                overwrite, 
                subDirectoryDepth, 
                rename, 
                fileFormat, 
                directoryFormat, 
                directorySeperator);
        }
        
    }
}