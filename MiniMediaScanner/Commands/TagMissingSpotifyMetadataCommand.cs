using ConsoleAppFramework;

namespace MiniMediaScanner.Commands;

public class TagMissingSpotifyMetadataCommand
{
    /// <summary>
    /// Tag missing metadata using Spotify, optionally write to file
    /// </summary>
    /// <param name="connectionString">-C, ConnectionString for Postgres database.</param>
    /// <param name="write">-w, Write missing metadata to media on disk.</param>
    /// <param name="artist">-a, Artistname.</param>
    /// <param name="album">-b, target Album.</param>
    /// <param name="overwritetag">-ow, Overwrite existing tag values.</param>
    [Command("tagmissingspotifymetadata")]
    public static void TagMissingMetadata(string connectionString, 
        bool write = false,
        string artist = "", 
        string album = "", 
        bool overwritetag = true)
    {
        var handler = new TagMissingSpotifyMetadataCommandHandler(connectionString);

        if (string.IsNullOrWhiteSpace(artist))
        {
            handler.TagMetadata(write, album, overwritetag);
        }
        else
        {
            handler.TagMetadata(write, artist, album, overwritetag);
        }
    }
}