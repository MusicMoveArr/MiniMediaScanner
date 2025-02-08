using System.Diagnostics;
using MiniMediaScanner.Models;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class FingerPrintMediaCommandHandler
{
    private readonly FingerPrintService _fingerPrintService;
    private readonly MetadataRepository _metadataRepository;
    private readonly ArtistRepository _artistRepository;
    private readonly MediaTagWriteService _mediaTagWriteService;

    private Stopwatch sw = Stopwatch.StartNew();
    private int generatedFingers = 0;
    public FingerPrintMediaCommandHandler(string connectionString)
    {
        _fingerPrintService = new FingerPrintService();
        _metadataRepository = new MetadataRepository(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
        _mediaTagWriteService = new MediaTagWriteService();
    }
    
    public void FingerPrintMedia(string album)
    {
        _artistRepository.GetAllArtistNames()
            .ForEach(artist => FingerPrintMedia(artist, album));
    }
    
    public void FingerPrintMedia(string artist, string album)
    {
        Console.WriteLine($"Processing artist '{artist}'");
        var metadata = _metadataRepository.GetAllMetadataPathsByMissingFingerprint(artist)
            .Where(metadata => string.IsNullOrWhiteSpace(album) || string.Equals(metadata.AlbumName, album, StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        metadata
            .AsParallel()
            .WithDegreeOfParallelism(4)
            .ForAll(metadata =>
            {
                try
                {
                    FingerPrintFile(metadata);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            });
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
            _mediaTagWriteService.SaveTag(new FileInfo(metadata.Path), "acoustid fingerprint", fingerprint.Fingerprint);
            FileInfo fileInfo = new FileInfo(metadata.Path);
            _metadataRepository.UpdateMetadataFingerprint(metadata.MetadataId.ToString(), fingerprint.Fingerprint, fingerprint.Duration, fileInfo.LastWriteTime, fileInfo.CreationTime);
        }
    }
}