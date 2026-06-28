using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace MiniMediaScanner.Services.AnalyseSonic;
public static class MelSpectrogram
{
    public const int SampleRate = 16000;
    public const int FftSize = 512;
    public const int HopSize = 256;
    public const int NumMelBands = 96;
    public const int PatchFrames = 128;
    public const int BatchSize = 64;

    public static List<float[,]> ComputePatches(float[] audio)
    {
        var frames = ComputeMelFrames(audio);
        if (frames.Count == 0) return new List<float[,]>();

        var patches = new List<float[,]>();
        for (int start = 0; start + PatchFrames <= frames.Count; start += PatchFrames )
        {
            var patch = new float[PatchFrames, NumMelBands];
            for (int t = 0; t < PatchFrames; t++)
            {
                for (int m = 0; m < NumMelBands; m++)
                {
                    patch[t, m] = frames[start + t][m];
                }
            }
            
            patches.Add(patch);
        }
        return patches;
    }

    private static List<float[]> ComputeMelFrames(float[] audio)
    {
        var frames    = new List<float[]>();
        var melFilter = BuildMelFilterbank();

        for (int i = 0; i + FftSize <= audio.Length; i += HopSize)
        {
            var complex = new System.Numerics.Complex[FftSize];
            for (int j = 0; j < FftSize; j++)
            {
                double hann = 0.5 * (1 - Math.Cos(2 * Math.PI * j / (FftSize - 1)));
                complex[j]  = new System.Numerics.Complex(audio[i + j] * hann, 0);
            }

            Fourier.Forward(complex);

            var power = new float[FftSize / 2 + 1];
            for (int j = 0; j < power.Length; j++)
                power[j] = (float)complex[j].MagnitudeSquared();

            var melFrame = new float[NumMelBands];
            for (int m = 0; m < NumMelBands; m++)
            {
                float sum = 0;
                for (int j = 0; j < power.Length; j++)
                    sum += melFilter[m][j] * power[j];

                float compressed = (float)Math.Log10(1f + 10000f * sum + 1f);
                melFrame[m] = compressed;
            }
            frames.Add(melFrame);
        }
        return frames;
    }

    private static float[][] BuildMelFilterbank()
    {
        double HzToMel(double hz) => 2595 * Math.Log10(1 + hz / 700.0);
        double MelToHz(double mel) => 700 * (Math.Pow(10, mel / 2595.0) - 1);

        double melMin  = HzToMel(0);
        double melMax  = HzToMel(SampleRate / 2.0 - 1);
        int    numBins = FftSize / 2 + 1;

        var melPoints = Enumerable.Range(0, NumMelBands + 2)
            .Select(i => MelToHz(melMin + i * (melMax - melMin) / (NumMelBands + 1)))
            .Select(hz => (int)Math.Floor(hz * (FftSize + 1) / SampleRate))
            .ToArray();

        var filters = new float[NumMelBands][];
        for (int m = 1; m <= NumMelBands; m++)
        {
            filters[m - 1] = new float[numBins];
            int left   = melPoints[m - 1];
            int center = melPoints[m];
            int right  = melPoints[m + 1];
            for (int k = Math.Max(0, left); k < numBins && k <= right; k++)
            {
                if (k <= center && center > left)
                {
                    filters[m - 1][k] = (k - left) / (float)(center - left);
                }
                else if (k > center && right > center)
                {
                    filters[m - 1][k] = (right - k) / (float)(right - center);
                }
            }
        }
        return filters;
    }
}