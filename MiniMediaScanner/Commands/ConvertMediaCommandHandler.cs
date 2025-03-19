using System.Diagnostics;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;
using Newtonsoft.Json;

namespace MiniMediaScanner.Commands;

public class ConvertMediaCommandHandler
{
    private const int FfMpegSuccessCode = 0;
    private TimeSpan ConversionTimeout = TimeSpan.FromMinutes(5);
    private readonly FingerPrintService _fingerPrintService;
    private readonly MetadataRepository _metadataRepository;

    public ConvertMediaCommandHandler(string connectionString)
    {
        _fingerPrintService = new FingerPrintService();
        _metadataRepository = new MetadataRepository(connectionString);
    }

    public async Task ConvertAllArtistsAsync(string fromExtension, string toExtension, string codec,  string bitrate)
    {
        var metadataFiles = (await _metadataRepository.GetMetadataByFileExtensionAsync(fromExtension));

        await ParallelHelper.ForEachAsync(metadataFiles, 4, async metadata =>
        {
            try
            {
                await ProcessFileAsync(metadata, toExtension, codec, bitrate);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        });
    }
    
    public async Task ConvertByArtistAsync(string fromExtension, string toExtension, string artist, string codec,  string bitrate)
    {
        var metadataFiles = (await _metadataRepository.GetMetadataByArtistAsync(artist))
            .Where(metadata => metadata.Path.EndsWith(fromExtension))
            .ToList();

        await ParallelHelper.ForEachAsync(metadataFiles, 4, async metadata =>
        {
            try
            {
                await ProcessFileAsync(metadata, toExtension, codec, bitrate);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        });
    }

    private async Task ProcessFileAsync(MetadataModel metadata, string toExtension, string codec,  string bitrate)
    {
        FileInfo file = new FileInfo(metadata.Path);

        if (!file.Exists)
        {
            Console.WriteLine($"File does not exist anymore '{file.FullName}'");
            return;
        }

        DirectoryInfo directory = file.Directory;
        FileInfo outputFile = new FileInfo(Path.Combine(directory.FullName, file.Name.Substring(0, file.Name.Length - file.Extension.Length) + $".{toExtension}"));

        if (outputFile.Exists)
        {
            var existingMetadata = await _metadataRepository.GetMetadataByPathAsync(outputFile.FullName);

            foreach (var record in existingMetadata)
            {
                await _metadataRepository.DeleteMetadataRecordsAsync(new List<string>() { record.MetadataId.ToString() });
            }
            outputFile.Delete();
            Console.WriteLine($"Deleted target, records deleted '{existingMetadata.Count}', file already exists, '{outputFile.FullName}'");
        }

        bool success = ConvertFile(file, outputFile.FullName, codec, bitrate);
        if(success)
        {
            Console.WriteLine($"Successfully converted '{file.FullName}' to '{outputFile.FullName}'");
            await _metadataRepository.UpdateMetadataPathAsync(metadata.MetadataId.ToString(), outputFile.FullName); 

            FpcalcOutput? fingerprint = await _fingerPrintService.GetFingerprintAsync(outputFile.FullName);

            if (fingerprint != null)
            {
                FileInfo fileInfo = new FileInfo(outputFile.FullName);
                await _metadataRepository.UpdateMetadataFingerprintAsync(metadata.MetadataId.ToString(), fingerprint.Fingerprint, fingerprint.Duration, fileInfo.LastWriteTime, fileInfo.CreationTime);
            }
            
            file.Delete();
        }
        else
        {
            Console.WriteLine($"Failed to convert file '{file.FullName}' to '{outputFile.FullName}'");
        }
    }

    private bool ConvertFile(FileInfo input, string output, string codec, string bitrate)
    {
        ProcessStartInfo ffmpegStartInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-hide_banner -y -vn -hwaccel auto -nostdin -i \"{input.FullName}\" -c:a {codec} -b:a {bitrate} -map_metadata 0 -movflags +faststart -map_metadata 0:s:0 \"{output}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        Process ffmpegProcess = Process.Start(ffmpegStartInfo);

        if (!ffmpegProcess.WaitForExit(ConversionTimeout))
        {
            Console.WriteLine($"Conversion timed out for file '{input.FullName}'");
            ffmpegProcess.Kill();
            
            if (File.Exists(output))
            {
                File.Delete(output);
            }
            return false;
        }
        
        if (ffmpegProcess.ExitCode != FfMpegSuccessCode)
        {
            if (File.Exists(output))
            {
                File.Delete(output);
            }
            return false;
        }
        return true;
    }
}