using System.Text.RegularExpressions;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class MissingCommandHandler
{
    private readonly DatabaseService _databaseService;

    public MissingCommandHandler(string connectionString)
    {
        _databaseService = new DatabaseService(connectionString);
    }
    
    public void CheckMissingTracksByArtist(string artistName)
    {
        _databaseService.GetMissingTracksByArtist(artistName)
            .ToList()
            .ForEach(track =>
            {
                Console.WriteLine(track);
            });
    }
    
    public void CheckAllMissingTracks()
    {
        var filteredNames = _databaseService.GetAllArtistNames();

        foreach (string artistName in filteredNames)
        {
            var missingTracks = _databaseService.GetMissingTracksByArtist(artistName);

            missingTracks.ForEach(track =>
            {
                Console.WriteLine(track);
            });
        }
    }
}