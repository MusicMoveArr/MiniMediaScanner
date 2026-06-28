using FFMpegCore;

namespace MiniMediaScanner.Services.AnalyseSonic;

public static class AudioLoader
{
    public const int TargetSampleRate = 16000;

    public static float[] LoadMono(string path)
    {
        //decode to raw 16kHz mono PCM f32le
        var tempFile = Path.GetTempFileName() + ".raw";
        try
        {
            FFMpegArguments
                .FromFileInput(path)
                .OutputToFile(tempFile, overwrite: true, options => options
                    .WithAudioSamplingRate(TargetSampleRate)
                    .WithCustomArgument("-ac 1") // mono
                    .WithCustomArgument("-f f32le") // 32-bit float PCM
                    .WithCustomArgument("-acodec pcm_f32le"))
                .ProcessSynchronously();

            var bytes  = File.ReadAllBytes(tempFile);
            var floats = new float[bytes.Length / 4];
            Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
            
            float maxAbs = floats.Max(Math.Abs);
            if (maxAbs > 1e-8f)
            {
                for (int i = 0; i < floats.Length; i++)
                {
                    floats[i] /= maxAbs;
                }
            }
            
            return floats;
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}