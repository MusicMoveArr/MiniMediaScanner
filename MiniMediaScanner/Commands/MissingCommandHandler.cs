using System.Text.RegularExpressions;
using FuzzySharp;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class MissingCommandHandler
{
    private readonly ArtistRepository _artistRepository;
    private readonly MetadataRepository _metadataRepository;
    private readonly MissingRepository _missingRepository;

    public MissingCommandHandler(string connectionString)
    {
        _artistRepository = new ArtistRepository(connectionString);
        _metadataRepository = new MetadataRepository(connectionString);
        _missingRepository = new MissingRepository(connectionString);
    }
    
    public void CheckMissingTracksByArtist(string artistName)
    {
        /*var musicBrainzRecords = _missingRepository.GetMusicBrainzRecords(artistName);
        var metadata = _missingRepository.GetMetadataByArtist(artistName);
        //var associatedArtists = _missingRepository.GetAssociatedArtists(artistName);

        foreach (var musicBrainzRecord in musicBrainzRecords)
        {
            //var targetMetadata = metadata
            //    .FirstOrDefault(m =>
            //        string.Equals(m.Title, musicBrainzRecord.TrackTitle, StringComparison.OrdinalIgnoreCase));
            var targetMetadata = metadata
                .FirstOrDefault(m =>
                    Fuzz.Ratio(m.Title.ToLower(), musicBrainzRecord.TrackTitle.ToLower()) > 90);

            if (targetMetadata == null)
            {
                Console.WriteLine($"{musicBrainzRecord.ArtistName} - {musicBrainzRecord.AlbumTitle} - {musicBrainzRecord.TrackTitle}");
            }
        }*/
        
        /*var artistNames = _artistRepository.GetArtistNamesCaseInsensitive(artistName);
        
        _metadataRepository.GetMissingTracksByArtist(artistNames)
            .ToList()
            .ForEach(track =>
            {
                Console.WriteLine(track);
            });*/
        
        var missingTracks = _missingRepository.GetMissingTracksByArtistSpotify(artistName);

        missingTracks.ForEach(track =>
        {
            Console.WriteLine(track);
        });
    }
    
    public void CheckAllMissingTracks()
    {
        var filteredNames = _artistRepository.GetAllArtistNames();

        foreach (string artistName in filteredNames)
        {
            var missingTracks = _missingRepository.GetMissingTracksByArtistSpotify(artistName);

            missingTracks.ForEach(track =>
            {
                Console.WriteLine(track);
            });
        }
    }
}