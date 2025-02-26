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

    public void ShowStats()
    {
        Console.WriteLine($"Artists: {_statsRepository.GetGenericCount("artists")}");
        Console.WriteLine($"Albums: {_statsRepository.GetGenericCount("albums")}");
        Console.WriteLine($"Tracks: {_statsRepository.GetGenericCount("metadata")}");
        Console.WriteLine($"Tracks added last 1day: {_statsRepository.GetTracksAddedCount(1)}");
        Console.WriteLine($"Tracks added last 7days: {_statsRepository.GetTracksAddedCount(7)}");
        
        Console.WriteLine($"Cached MusicBrainz Artists: {_statsRepository.GetGenericCount("musicbrainzartist")}");
        Console.WriteLine($"Cached MusicBrainz Albums: {_statsRepository.GetGenericCount("musicbrainzrelease")}");
        Console.WriteLine($"Cached MusicBrainz Tracks: {_statsRepository.GetGenericCount("musicbrainzreleasetrack")}");
        
    }
}