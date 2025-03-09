using ConsoleAppFramework;

namespace MiniMediaScanner.Commands;

public class CoverArtSpotifyCommand
{
    /// <summary>
    /// Download Cover art from Spotify for Artist and Album
    /// </summary>
    /// <param name="connectionString">-C, ConnectionString for Postgres database.</param>
    /// <param name="artist">-a, Artistname.</param>
    /// <param name="album">-b, target Album.</param>
    /// <param name="albumfilename">-f, File name e.g. cover.jpg.</param>
    /// <param name="artistfilename">-af, File name e.g. cover.jpg.</param>
    [Command("coverartspotify")]
    public static void CoverArtSpotify(string connectionString,
        string artist = "",
        string album = "",
        string albumfilename = "cover.jpg",
        string artistfilename = "cover.jpg")
    {
        var handler = new CoverArtSpotifyCommandHandler(connectionString);

        if (string.IsNullOrWhiteSpace(artist))
        {
            handler.CheckAllMissingCovers(album, albumfilename, artistfilename);
        }
        else
        {
            handler.CheckAllMissingCovers(artist, album, albumfilename, artistfilename);
        }
    }
}