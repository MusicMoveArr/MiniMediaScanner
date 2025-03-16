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
    
    public async Task CheckMissingTracksByArtistAsync(string artistName, string provider)
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

        try
        {
            var missingTracks = provider.ToLower() == "spotify" ? 
                await _missingRepository.GetMissingTracksByArtistSpotify2Async(artistName) :
                await _missingRepository.GetMissingTracksByArtistMusicBrainz2Async(artistName);
        
            missingTracks.ForEach(track =>
            {
                Console.WriteLine(track);
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;  
        }
    }
    
    public async Task CheckAllMissingTracksAsync(string provider)
    {
        var filteredNames = await _artistRepository.GetAllArtistNamesAsync();

        foreach (string artistName in filteredNames)
        {
            var missingTracks = provider.ToLower() == "spotify" ? 
                await _missingRepository.GetMissingTracksByArtistSpotify2Async(artistName) :
                await _missingRepository.GetMissingTracksByArtistMusicBrainz2Async(artistName);

            missingTracks.ForEach(track =>
            {
                Console.WriteLine(track);
            });
        }
    }
}