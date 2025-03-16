using System.Diagnostics;
using MiniMediaScanner.Models;
using Newtonsoft.Json;

namespace MiniMediaScanner.Services;

public class FingerPrintService
{
    public async Task<FpcalcOutput?> GetFingerprintAsync(string filePath)
    {
        // Start a new process for fpcalc
        string escapedFilePath = filePath.Replace("\"", "\\\"");
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "fpcalc",  // Command to call
                Arguments = $"-json \"{escapedFilePath}\"",  // Path to the audio file
                RedirectStandardOutput = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        // Start the process and read the output
        process.Start();
        await process.WaitForExitAsync();
        string output = await process.StandardOutput.ReadToEndAsync();
        string err = await process.StandardError.ReadToEndAsync();

        return ParseFingerprint(output);
    }

    private FpcalcOutput? ParseFingerprint(string output)
    {
        // Deserialize JSON output to FpcalcOutput object
        var result = JsonConvert.DeserializeObject<FpcalcOutput>(output);
        
        if (result == null || string.IsNullOrEmpty(result.Fingerprint))
        {
            return null;
        }
        
        return result;
    }
}