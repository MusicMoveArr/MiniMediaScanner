using System.Text.RegularExpressions;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class MissingCommandHandler
{
    private readonly ArtistRepository _artistRepository;
    private readonly MetadataRepository _metadataRepository;

    public MissingCommandHandler(string connectionString)
    {
        _artistRepository = new ArtistRepository(connectionString);
        _metadataRepository = new MetadataRepository(connectionString);
    }
    
    public void CheckMissingTracksByArtist(string artistName)
    {
        _metadataRepository.GetMissingTracksByArtist(artistName)
            .ToList()
            .ForEach(track =>
            {
                Console.WriteLine(track);
            });
    }
    
    public void CheckAllMissingTracks()
    {
        var filteredNames = _artistRepository.GetAllArtistNames();

        foreach (string artistName in filteredNames)
        {
            var missingTracks = _metadataRepository.GetMissingTracksByArtist(artistName);

            missingTracks.ForEach(track =>
            {
                Console.WriteLine(track);
            });
        }
    }
}