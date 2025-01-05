using System.Diagnostics;
using MiniMediaScanner.Models;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class FingerPrintMediaCommandHandler
{
    private readonly DatabaseService _databaseService;
    private readonly FingerPrintService _fingerPrintService;

    private Stopwatch sw = Stopwatch.StartNew();
    private int generatedFingers = 0;
    public FingerPrintMediaCommandHandler(string connectionString)
    {
        _databaseService = new DatabaseService(connectionString);
        _fingerPrintService = new FingerPrintService();
    }
    
    public void FingerPrintMedia()
    {
        int missingCount = 0;
        const int limit = 1000;
        int offset = 0;
        while (true)
        {
            var metadata = _databaseService.GetAllMetadataPathsByMissingFingerprint(offset, limit);
            offset += limit;

            if (metadata.Count == 0)
            {
                break;
            }
            
            metadata
                .AsParallel()
                .WithDegreeOfParallelism(4)
                .ForAll(metadata => FingerPrintFile(metadata));
        }
        Console.WriteLine($"Total missing media files: {missingCount}");
    }

    private void FingerPrintFile(MetadataModel metadata)
    {
        if (sw.Elapsed.Seconds >= 5)
        {
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] Generated FingerPrints: {generatedFingers}");
            generatedFingers = 0;
            sw.Restart();
        }
        
        FpcalcOutput? fingerprint = _fingerPrintService.GetFingerprint(metadata.Path);
        if (!string.IsNullOrWhiteSpace(fingerprint?.Fingerprint))
        {
            generatedFingers++;
            _databaseService.UpdateMetadataFingerprint(metadata.MetadataId, fingerprint.Fingerprint, fingerprint.Duration);
        }
    }
}