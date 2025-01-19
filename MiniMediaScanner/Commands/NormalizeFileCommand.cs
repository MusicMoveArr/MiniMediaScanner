using ConsoleAppFramework;

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
    /// <param name="normalizeArtistName">-a, normalize Artistname.</param>
    /// <param name="normalizeAlbumName">-b, normalize Albumname.</param>
    /// <param name="normalizeTitleName">-t, normalize music Title.</param>
    /// <param name="rename">-r, rename file.</param>
    /// <param name="overwrite">-w, overwrite existing files.</param>
    /// <param name="subDirectoryDepth">-s, sub-directory depth to root-folder.</param>
    /// <param name="fileFormat">-f, rename file format (required for renaming) {artist} {album} {track} {title}.</param>
    /// <param name="directoryFormat">-df, rename directory format (required for renaming) {artist} {album} {track} {title}.</param>
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
        string directorySeperator = "_")
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
        
        //run small test to see if format is correct
        string newFileName = handler.GetFormatName(fileFormat, 
            "artistName", 
            "albumName", 
            5,
            "someTitle",
            directorySeperator);
        
        if (newFileName.Contains("{") || newFileName.Contains("}"))
        {
            Console.WriteLine("File Format is invalid.");
            return;
        }
        
        //run small test to see if format is correct
        string newDirectoryName = handler.GetFormatName(directoryFormat, 
            "artistName", 
            "albumName", 
            5,
            "someTitle",
            directorySeperator);
        
        if (newDirectoryName.Contains("{") || newDirectoryName.Contains("}"))
        {
            Console.WriteLine("Directory Format is invalid.");
            return;
        }

        handler.NormalizeFiles(normalizeArtistName, normalizeAlbumName, normalizeTitleName, overwrite, subDirectoryDepth, rename, fileFormat, directoryFormat, directorySeperator);
    }
}