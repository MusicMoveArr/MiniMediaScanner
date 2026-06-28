using MiniMediaScanner.Models.MoodAnalysis;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace MiniMediaScanner.Services.AnalyseSonic;
public class EssentiaTFModel : IDisposable
{
    private readonly InferenceSession _session;
    private readonly string _inputName;
    public string FilePath { get; private set; }
    public OnnxModel OnnxModel { get; private set; }

    public EssentiaTFModel(string onnxPath)
    {
        FilePath = onnxPath;
        
        var jsonPath = new FileInfo(onnxPath).FullName.Replace(".onnx", ".json");
        if (File.Exists(jsonPath))
        {
            var json = File.ReadAllText(jsonPath);
            OnnxModel = System.Text.Json.JsonSerializer.Deserialize<OnnxModel>(json);
        }
        
        _session    = new InferenceSession(onnxPath);
        _inputName  = _session.InputMetadata.Keys.First();
    }

    public float[] RunEffnet(List<float[,]> patches)
    {
        if (patches.Count == 0)
        {
            return new float[1280];
        }
        
        int embDim = 0;
        float[]? mean = null;
        int totalBatches = 0;

        for (int batchStart = 0; batchStart < patches.Count; batchStart += MelSpectrogram.BatchSize)
        {
            int batchCount = Math.Min(MelSpectrogram.BatchSize, patches.Count - batchStart);
            int frames = MelSpectrogram.PatchFrames;
            int bands = MelSpectrogram.NumMelBands;

            var flat = new float[MelSpectrogram.BatchSize * frames * bands];
            for (int b = 0; b < batchCount; b++)
            {
                var p = patches[batchStart + b];
                for (int t = 0; t < frames; t++)
                {
                    for (int m = 0; m < bands; m++)
                    {
                        flat[b * frames * bands + t * bands + m] = p[t, m];
                    }
                }
            }

            var tensor = new DenseTensor<float>(flat,new[] { MelSpectrogram.BatchSize, frames, bands });

            using var results = _session.Run(new[]
            {
                NamedOnnxValue.CreateFromTensor(_inputName, tensor)
            });

            var output = results.First().AsEnumerable<float>().ToArray();
            embDim = output.Length / MelSpectrogram.BatchSize;

            if (mean == null)
            {
                mean = new float[embDim];
            }

            for (int b = 0; b < batchCount; b++)
            {
                for (int i = 0; i < embDim; i++)
                {
                    mean[i] += output[b * embDim + i];
                }
            }
            totalBatches += batchCount;
        }

        // Divide to get mean
        for (int i = 0; i < mean!.Length; i++)
        {
            mean[i] /= totalBatches;
        }
        return mean;
    }

    public float[] RunEmbedding(float[] embedding)
    {
        var tensor = new DenseTensor<float>(embedding, new[] { 1, embedding.Length });
        using var results = _session.Run(
            new[] { NamedOnnxValue.CreateFromTensor(_inputName, tensor) });
        return results.First().AsEnumerable<float>().ToArray();
    }

    public void Dispose() => _session.Dispose();
}