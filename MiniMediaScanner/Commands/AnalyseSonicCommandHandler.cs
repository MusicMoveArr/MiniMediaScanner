using System.Diagnostics;
using ListRandomizer;
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
    private readonly MoodAnalyzer _moodAnalyzer;

    private bool _refreshedCount = false;
    private int _leftToProcess = 0;
    private readonly Stopwatch _refreshStopwatch = Stopwatch.StartNew();

    public AnalyseSonicCommandHandler(string connectionString, int threads)
    {
        _threads = threads;
        _metadataRepository = new MetadataRepository(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
        _metadataSonicRepository = new MetadataSonicRepository(connectionString);
        _moodAnalyzer = new MoodAnalyzer("./MachineLearningModels");
    }

    public async Task CheckAllTracksAsync(string album)
    {
        //shuffling on purpose in case users (including myself) want to use multiple machines to use Machine Learning
        //to speed up the process. Personally I went from 10 threads to 20 threads using 2 machines
        var artists = await _artistRepository.GetAllArtistNamesAsync();
        artists.Shuffle();
        await ParallelHelper.ForEachAsync(artists, 1, async artist =>
        {
            try
            {
                await CheckAllTracksAsync(artist, album);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        });
    }
    
    public async Task CheckAllTracksAsync(string artist, string album)
    {
        var metadata = (await _metadataSonicRepository.GetTracksToProcessAsync(artist))
            .Where(metadata => string.IsNullOrWhiteSpace(album) || string.Equals(metadata.AlbumName, album, StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        if (metadata.Count == 0)
        {
            return;
        }
        
        var tracksToProcess = metadata
            .Where(metadata => new FileInfo(metadata.Path).Exists)
            .ToList();

        if (!_refreshedCount)
        {
            _leftToProcess = await _metadataSonicRepository.GetCountToProcessAsync();
            _refreshedCount = true;
        }

        await Parallel.ForEachAsync(tracksToProcess, 
            new ParallelOptions { MaxDegreeOfParallelism = _threads },
            async (track, token) =>
            {
                try
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    var embedding = _moodAnalyzer.GetEmbedding(track.Path);

                    if (!embedding.Any())
                    {
                        return;
                    }
                    
                    var moods = _moodAnalyzer.AnalyzeModels(embedding);
            
                    MetadataMoodModel moodModel = new MetadataMoodModel
                    {
                        MetadataId = track.MetadataId,
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

                    if (_refreshStopwatch.Elapsed.TotalSeconds >= 10.0D)
                    {
                        _refreshStopwatch.Restart();
                        _leftToProcess = await _metadataSonicRepository.GetCountToProcessAsync();
                    }
                    
                    Console.WriteLine($"Analysed, {sw.ElapsedMilliseconds}msec, left to process: {_leftToProcess}, {track.Path}");
                    await _metadataSonicRepository.UpsertMetadataMoodAsync(moodModel);
                    await _metadataSonicRepository.UpdateMetadataMoodVectorAsync(track.MetadataId);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
    }
}