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
    private readonly TidalRepository _tidalRepository;
    
    public MissingCommandHandler(string connectionString)
    {
        _artistRepository = new ArtistRepository(connectionString);
        _metadataRepository = new MetadataRepository(connectionString);
        _missingRepository = new MissingRepository(connectionString);
        _matchRepository = new MatchRepository(connectionString);
        _tidalRepository = new TidalRepository(connectionString);
    }
    
    public async Task CheckMissingTracksByArtistAsync(
        string artistName, 
        string provider, 
        string output, 
        List<string>? filterOut, 
        string extension, 
        string filePath, 
        bool fileAppend)
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
                        tempMissingTracks = await _missingRepository.GetMissingTracksByArtistSpotify2Async(spotifyArtistId, artistName, extension);
                    }
                }
            }
            else if (provider.ToLower() == "deezer")
            {
                var artistIds = await _metadataRepository.GetArtistIdByMetadataAsync(artistName);
                Guid? artistId = artistIds.FirstOrDefault(track => track.HasValue);
                if (GuidHelper.GuidHasValue(artistId))
                {
                    long? deezerArtistId = await _matchRepository.GetBestDeezerMatchAsync(artistId.Value, artistName);
                    if (deezerArtistId > 0)
                    {
                        tempMissingTracks = await _missingRepository.GetMissingTracksByArtistDeezerAsync(deezerArtistId.Value, artistName, extension);
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
                        tempMissingTracks = await _missingRepository.GetMissingTracksByArtistTidalAsync(tidalArtistId.Value, artistName, extension);
                    }
                }
            }
            else
            {
                tempMissingTracks = await _missingRepository.GetMissingTracksByArtistMusicBrainz2Async(artistName, extension);
            }

            //re-check with Associated Artists
            List<MissingTrackModel> missingTracks = new List<MissingTrackModel>();
            foreach (var track in tempMissingTracks)
            {
                if (!await _missingRepository.TrackExistsAtAssociatedArtist(track.Artist, track.Album, track.Track, extension))
                {
                    missingTracks.Add(track);
                }
            }

            StreamWriter? fileStream = null;
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                fileStream = new StreamWriter(filePath, fileAppend);
            }
            
            missingTracks
                .Where(track => filterOut?.Count == 0 || !filterOut.Any(filter => $"{track.Album} - {track.Track}".ToLower().Contains(filter.ToLower())))
                .Select(track => SmartFormat.Smart.Format(output, track))
                .Where(track => filterOut?.Count == 0 || !filterOut.Any(filter => track.ToLower().Contains(filter.ToLower())))
                .Distinct()
                .ToList()
                .ForEach(track =>
                {
                    Console.WriteLine(track);
                    fileStream?.WriteLine(track);
                });
            fileStream?.Flush();
            fileStream?.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;  
        }
    }
    
    public async Task CheckAllMissingTracksAsync(
        string provider, 
        string output, 
        List<string>? filterOut, 
        string extension, 
        string filePath, 
        bool fileAppend)
    {
        var filteredNames = await _artistRepository.GetAllArtistNamesAsync();

        foreach (string artistName in filteredNames)
        {
            await CheckMissingTracksByArtistAsync(artistName, provider, output, filterOut, extension, filePath, fileAppend);
        }
    }
}