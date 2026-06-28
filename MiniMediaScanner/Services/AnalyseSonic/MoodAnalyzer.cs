namespace MiniMediaScanner.Services.AnalyseSonic;

public class MoodAnalyzer : IDisposable
{
    private readonly EssentiaTFModel _effnet;
    private readonly Dictionary<string, EssentiaTFModel> _moodModels;

    public MoodAnalyzer(string modelDir)
    {
        _effnet = new EssentiaTFModel(Path.Combine(modelDir, "discogs-effnet.onnx")); //which is "discogs-effnet-bs64-1.pb" but converted to .onnx

        _moodModels = new Dictionary<string, EssentiaTFModel>
        {
            ["happy"]      = new EssentiaTFModel(Path.Combine(modelDir, "mood_happy-discogs-effnet-1.onnx")),
            ["sad"]        = new EssentiaTFModel(Path.Combine(modelDir, "mood_sad-discogs-effnet-1.onnx")),
            ["aggressive"] = new EssentiaTFModel(Path.Combine(modelDir, "mood_aggressive-discogs-effnet-1.onnx")),
            ["relaxed"]    = new EssentiaTFModel(Path.Combine(modelDir, "mood_relaxed-discogs-effnet-1.onnx")),
            ["acoustic"]    = new EssentiaTFModel(Path.Combine(modelDir, "mood_acoustic-discogs-effnet-1.onnx")),
            ["electronic"]    = new EssentiaTFModel(Path.Combine(modelDir, "mood_electronic-discogs-effnet-1.onnx")),
            ["party"]    = new EssentiaTFModel(Path.Combine(modelDir, "mood_party-discogs-effnet-1.onnx")),
            ["approachability"]    = new EssentiaTFModel(Path.Combine(modelDir, "approachability_3c-discogs-effnet-1.onnx")),
            ["danceability"]    = new EssentiaTFModel(Path.Combine(modelDir, "danceability-discogs-effnet-1.onnx")),
            ["voice_instrumental"]    = new EssentiaTFModel(Path.Combine(modelDir, "voice_instrumental-discogs-effnet-1.onnx")),
            ["genre"]    = new EssentiaTFModel(Path.Combine(modelDir, "genre_discogs400-discogs-effnet-1.onnx")),
            ["timbre"]    = new EssentiaTFModel(Path.Combine(modelDir, "timbre-discogs-effnet-1.onnx")),
            ["engagement_3c"]    = new EssentiaTFModel(Path.Combine(modelDir, "engagement_3c-discogs-effnet-1.onnx")),
            ["engagement_regression"]    = new EssentiaTFModel(Path.Combine(modelDir, "engagement_regression-discogs-effnet-1.onnx")),
            ["gender"]    = new EssentiaTFModel(Path.Combine(modelDir, "gender-discogs-effnet-1.onnx")),
        };
    }

    public float[] GetEmbedding(string audioPath)
    {
        var audio = AudioLoader.LoadMono(audioPath);
        var patches = MelSpectrogram.ComputePatches(audio);
        
        var embedding = _effnet.RunEffnet(patches);
        
        //L2 normalization
        float l2 = (float)Math.Sqrt(embedding.Sum(x => x * x) + 1e-12f);
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] /= l2;
        }
        return embedding;
    }

    public Dictionary<string, Dictionary<string, float>> AnalyzeModels(float[] embedding)
    {
        var results = new Dictionary<string, Dictionary<string, float>>();
        foreach (var (name, model) in _moodModels)
        {
            var scores = model.RunEmbedding(embedding);

            var tempResults = new Dictionary<string, float>();
            for (int i = 0; i < scores.Length; i++)
            {
                tempResults.Add(model.OnnxModel.classes[i], scores[i]);
            }

            results[name] = tempResults;
        }
        return results;
    }

    public void Dispose()
    {
        _effnet.Dispose();
        foreach (var m in _moodModels.Values) m.Dispose();
    }
}