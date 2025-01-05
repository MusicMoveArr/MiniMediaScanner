using System.Diagnostics;
using MiniMediaScanner.Models;
using MiniMediaScanner.Services;
using Newtonsoft.Json;

namespace MiniMediaScanner.Commands;

public class ConvertMediaCommandHandler
{
    private readonly DatabaseService _databaseService;
    private const int FfMpegSuccessCode = 0;
    private TimeSpan ConversionTimeout = TimeSpan.FromMinutes(5);
    private readonly FingerPrintService _fingerPrintService;

    public ConvertMediaCommandHandler(string connectionString)
    {
        _databaseService = new DatabaseService(connectionString);
        _fingerPrintService = new FingerPrintService();
    }

    public void ConvertAllArtists(string fromExtension, string toExtension, string codec,  string bitrate)
    {
        var metadataFiles = _databaseService.GetMetadataByFileExtension(fromExtension);
        
        metadataFiles  
            .AsParallel()
            .WithDegreeOfParallelism(4)
            .ForAll(metadata => ProcessFile(metadata, toExtension, codec, bitrate));
    }
    
    public void ConvertByArtist(string fromExtension, string toExtension, string artist, string codec,  string bitrate)
    {
        var metadataFiles = _databaseService.GetMetadataByArtist(artist)
            .Where(metadata => metadata.Path.EndsWith(fromExtension))
            .ToList();

        metadataFiles  
            .AsParallel()
            .WithDegreeOfParallelism(30)
            .ForAll(metadata => ProcessFile(metadata, toExtension, codec, bitrate));
        
    }

    private void ProcessFile(MetadataModel metadata, string toExtension, string codec,  string bitrate)
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
            var existingMetadata = _databaseService.GetMetadataByPath(outputFile.FullName);

            existingMetadata.ForEach(metadata =>  _databaseService.DeleteMetadataRecords(new List<string>() { metadata.MetadataId }));
            outputFile.Delete();
            Console.WriteLine($"Deleted target, records deleted '{existingMetadata.Count}', file already exists, '{outputFile.FullName}'");
        }

        bool success = ConvertFile(file, outputFile.FullName, codec, bitrate);
        if(success)
        {
            Console.WriteLine($"Successfully converted '{file.FullName}' to '{outputFile.FullName}'");
            _databaseService.UpdateMetadataPath(metadata.MetadataId, outputFile.FullName); 

            FpcalcOutput? fingerprint = _fingerPrintService.GetFingerprint(outputFile.FullName);

            if (fingerprint != null)
            {
                _databaseService.UpdateMetadataFingerprint(metadata.MetadataId, fingerprint.Fingerprint, fingerprint.Duration);
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