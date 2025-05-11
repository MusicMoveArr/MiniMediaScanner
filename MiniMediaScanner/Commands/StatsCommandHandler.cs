using System.Diagnostics;
using MiniMediaScanner.Models;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;
using Newtonsoft.Json;

namespace MiniMediaScanner.Commands;

public class StatsCommandHandler
{
    private readonly StatsRepository _statsRepository;

    public StatsCommandHandler(string connectionString)
    {
        _statsRepository = new StatsRepository(connectionString);
    }

    public async Task ShowStatsAsync()
    {
        Console.WriteLine($"Artists: {await _statsRepository.GetGenericCountAsync("artists")}");
        Console.WriteLine($"Albums: {await _statsRepository.GetGenericCountAsync("albums")}");
        Console.WriteLine($"Tracks: {await _statsRepository.GetGenericCountAsync("metadata")}");
        Console.WriteLine($"Tracks added last 1day: {await _statsRepository.GetTracksAddedCountAsync(1)}");
        Console.WriteLine($"Tracks added last 7days: {await _statsRepository.GetTracksAddedCountAsync(7)}");
        
        Console.WriteLine($"Cached MusicBrainz Artists: {await _statsRepository.GetGenericCountAsync("musicbrainzartist")}");
        Console.WriteLine($"Cached MusicBrainz Albums: {await _statsRepository.GetGenericCountAsync("musicbrainzrelease")}");
        Console.WriteLine($"Cached MusicBrainz Tracks: {await _statsRepository.GetGenericCountAsync("musicbrainzreleasetrack")}");
        
        Console.WriteLine($"Cached Spotify Artists: {await _statsRepository.GetGenericCountAsync("spotify_artist")}");
        Console.WriteLine($"Cached Spotify Albums: {await _statsRepository.GetGenericCountAsync("spotify_album")}");
        Console.WriteLine($"Cached Spotify Tracks: {await _statsRepository.GetGenericCountAsync("spotify_track")}");
        
        Console.WriteLine($"Cached Tidal Artists: {await _statsRepository.GetGenericCountAsync("tidal_artist")}");
        Console.WriteLine($"Cached Tidal Albums: {await _statsRepository.GetGenericCountAsync("tidal_album")}");
        Console.WriteLine($"Cached Tidal Tracks: {await _statsRepository.GetGenericCountAsync("tidal_track")}");
        
    }
}