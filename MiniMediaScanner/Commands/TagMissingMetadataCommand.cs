using ConsoleAppFramework;

namespace MiniMediaScanner.Commands;

public class TagMissingMetadataCommand
{
    /// <summary>
    /// Tag missing metadata using AcousticBrainz, only tries already fingerprinted media, optionally write to file
    /// </summary>
    /// <param name="connectionString">-C, ConnectionString for Postgres database.</param>
    /// <param name="accoustId">-a, AccoustId API Key, required for getting data from MusicBrainz.</param>
    /// <param name="write">-w, Write missing metadata to media on disk.</param>
    [Command("tagmissingmetadata")]
    public static void TagMissingMetadata(string connectionString, string accoustId, bool write = false)
    {
        var handler = new TagMissingMetadataCommandHandler(connectionString);

        handler.FingerPrintMedia(accoustId, write);
    }
}