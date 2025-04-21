using System.Text.RegularExpressions;
using FuzzySharp;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class MissingCommandHandler
{
    private readonly ArtistRepository _artistRepository;
    private readonly MetadataRepository _metadataRepository;
    private readonly MissingRepository _missingRepository;
    private readonly MatchRepository _matchRepository;

    public MissingCommandHandler(string connectionString)
    {
        _artistRepository = new ArtistRepository(connectionString);
        _metadataRepository = new MetadataRepository(connectionString);
        _missingRepository = new MissingRepository(connectionString);
        _matchRepository = new MatchRepository(connectionString);
    }
    
    public async Task CheckMissingTracksByArtistAsync(string artistName, string provider, string output, List<string>? filterOut)
    {
        try
        {
            List<MissingTrackModel> tempMissingTracks = new List<MissingTrackModel>();
            
            if (provider.ToLower() == "spotify")
            {
                var artistIds = await _metadataRepository.GetArtistIdByMetadataAsync(artistName);
                Guid? artistId = artistIds.FirstOrDefault(track => track.HasValue);
                if (GuidHelper.GuidHasValue(artistId))
                {
                    string? spotifyArtistId = await _matchRepository.GetBestSpotifyMatchAsync(artistId.Value, artistName);
                    if (!string.IsNullOrWhiteSpace(spotifyArtistId))
                    {
                        tempMissingTracks = await _missingRepository.GetMissingTracksByArtistSpotify2Async(spotifyArtistId, artistName);
                    }
                }
            }
            else if (provider.ToLower() == "tidal")
            {
                var artistIds = await _metadataRepository.GetArtistIdByMetadataAsync(artistName);
                Guid? artistId = artistIds.FirstOrDefault(track => track.HasValue);
                if (GuidHelper.GuidHasValue(artistId))
                {
                    int? tidalArtistId = await _matchRepository.GetBestTidalMatchAsync(artistId.Value, artistName);
                    if (tidalArtistId > 0)
                    {
                        tempMissingTracks = await _missingRepository.GetMissingTracksByArtistTidalAsync(tidalArtistId.Value, artistName);
                    }
                }
            }
            else
            {
                tempMissingTracks = await _missingRepository.GetMissingTracksByArtistMusicBrainz2Async(artistName);
            }

            //re-check with Associated Artists
            List<MissingTrackModel> missingTracks = new List<MissingTrackModel>();
            foreach (var track in tempMissingTracks)
            {
                if (!await _missingRepository.TrackExistsAtAssociatedArtist(track.Artist, track.Album, track.Track))
                {
                    missingTracks.Add(track);
                }
            }
            
            missingTracks
                .Select(track => SmartFormat.Smart.Format(output, track))
                .Where(track => filterOut?.Count == 0 || !filterOut.Any(filter => track.ToLower().Contains(filter.ToLower())))
                .Distinct()
                .ToList()
                .ForEach(track =>
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
    
    public async Task CheckAllMissingTracksAsync(string provider, string output, List<string>? filterOut)
    {
        var filteredNames = await _artistRepository.GetAllArtistNamesAsync();

        foreach (string artistName in filteredNames)
        {
            await CheckMissingTracksByArtistAsync(artistName, provider, output, filterOut);
        }
    }
}