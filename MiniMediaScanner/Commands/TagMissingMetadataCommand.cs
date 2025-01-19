using ConsoleAppFramework;

namespace MiniMediaScanner.Commands;

public class TagMissingMetadataCommand
{
    /// <summary>
    /// Tag missing metadata using AcousticBrainz, only tries already fingerprinted media, optionally write to file
    /// </summary>
    /// <param name="connectionString">-C, ConnectionString for Postgres database.</param>
    /// <param name="accoustid">-A, AccoustId API Key, required for getting data from MusicBrainz.</param>
    /// <param name="write">-w, Write missing metadata to media on disk.</param>
    /// <param name="artist">-a, Artistname.</param>
    /// <param name="album">-b, target Album.</param>
    [Command("tagmissingmetadata")]
    public static void TagMissingMetadata(string connectionString, 
        string accoustid, 
        bool write = false,
        string artist = "", 
        string album = "")
    {
        var handler = new TagMissingMetadataCommandHandler(connectionString);

        if (string.IsNullOrWhiteSpace(artist))
        {
            handler.FingerPrintMedia(accoustid, write, album);
        }
        else
        {
            handler.FingerPrintMedia(accoustid, write, artist, album);
        }
    }
}