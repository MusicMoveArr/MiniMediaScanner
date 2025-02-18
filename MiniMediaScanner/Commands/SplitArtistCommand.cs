using ConsoleAppFramework;

namespace MiniMediaScanner.Commands;

public class SplitArtistCommand
{
    /// <summary>
    /// Split an artist the best we can based on MusicBrainzArtistId tag, if multiple artists use the same name
    /// Tags available: MusicBrainzRemoteId, Name, Country, Type, Date
    /// </summary>
    /// <param name="connectionString">-C, ConnectionString for Postgres database.</param>
    /// <param name="artist">-a, Artistname.</param>
    /// <param name="artistFormat">-af, artist format for splitting the 2 artists apart.</param>
    /// <param name="confirm">-y, Always confirm automatically.</param>
    [Command("splitartist")]
    public static void SplitArtist(string connectionString, 
        string artist, 
        string artistFormat,
        bool confirm = false)
    {
        var handler = new SplitArtistCommandHandler(connectionString);

        handler.SplitArtist(artist, artistFormat, confirm);
    }
}