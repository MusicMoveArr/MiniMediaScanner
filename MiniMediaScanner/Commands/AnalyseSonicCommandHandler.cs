using System.Text.RegularExpressions;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services.AnalyseSonic;
using Newtonsoft.Json;

namespace MiniMediaScanner.Commands;

public class AnalyseSonicCommandHandler
{
    private readonly MetadataRepository _metadataRepository;
    private readonly ArtistRepository _artistRepository;
    private readonly MetadataSonicRepository _metadataSonicRepository;
    private readonly int _threads;

    public AnalyseSonicCommandHandler(string connectionString, int threads)
    {
        _threads = threads;
        _metadataRepository = new MetadataRepository(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
        _metadataSonicRepository = new MetadataSonicRepository(connectionString);
    }

    public async Task CheckAllMissingTracksAsync(string album)
    {
        await ParallelHelper.ForEachAsync(await _artistRepository.GetAllArtistNamesAsync(), 1, async artist =>
        {
            try
            {
                await CheckAllMissingTracksAsync(artist, album);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        });
    }
    
    public async Task<int> CheckAllMissingTracksAsync(string artist, string album)
    {
        var metadata = (await _metadataRepository.GetMetadataByArtistAsync(artist))
            .Where(metadata => string.IsNullOrWhiteSpace(album) || string.Equals(metadata.AlbumName, album, StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        int missingCount = 0;

        if (metadata.Count == 0)
        {
            return 0;
        }
        
        var missing = metadata
            .Where(metadata => new FileInfo(metadata.Path).Exists)
            .ToList();

        using var analyzer = new MoodAnalyzer("./MachineLearningModels");

        await Parallel.ForEachAsync(missing, 
            new ParallelOptions { MaxDegreeOfParallelism = _threads },
            async (track, token) =>
            {
                if (await _metadataSonicRepository.MetadataMoodExistsAsync(track.MetadataId.Value))
                {
                    return;
                }
                
                try
                {
                    Console.WriteLine($"Analysing: {track.Path}");
                    var embedding = analyzer.GetEmbedding(track.Path);
                    var moods = analyzer.AnalyzeModels(embedding);
            
                    MetadataMoodModel moodModel = new MetadataMoodModel
                    {
                        MetadataId = track.MetadataId.Value,
                        Mood_Happy = JsonConvert.SerializeObject(moods["happy"]),
                        Mood_Sad = JsonConvert.SerializeObject(moods["sad"]),
                        Mood_Aggressive = JsonConvert.SerializeObject(moods["aggressive"]),
                        Mood_Relaxed = JsonConvert.SerializeObject(moods["relaxed"]),
                        Mood_Acoustic = JsonConvert.SerializeObject(moods["acoustic"]),
                        Mood_Electronic = JsonConvert.SerializeObject(moods["electronic"]),
                        Mood_Party = JsonConvert.SerializeObject(moods["party"]),
                        Ability_Approach =  JsonConvert.SerializeObject(moods["approachability"]),
                        Ability_Dance =  JsonConvert.SerializeObject(moods["danceability"]),
                        Voice_Instrumental = JsonConvert.SerializeObject(moods["voice_instrumental"]),
                        Engagement_3c =  JsonConvert.SerializeObject(moods["engagement_3c"]),
                        Engagement_Regression =  JsonConvert.SerializeObject(moods["engagement_regression"]),
                        Gender = JsonConvert.SerializeObject(moods["gender"]),
                        Genre_Json = JsonConvert.SerializeObject(moods["genre"].OrderByDescending(x => x.Value).Take(50).ToDictionary()),
                        Timbre = JsonConvert.SerializeObject(moods["timbre"]),
                    };

                    await _metadataSonicRepository.UpsertMetadataMoodAsync(moodModel);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
        
        return missingCount;
    }
}