using System.Diagnostics;
using MiniMediaScanner.Helpers;
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
    
    public async Task FingerPrintMediaAsync(string album)
    {
        await ParallelHelper.ForEachAsync(await _artistRepository.GetAllArtistNamesAsync(), 4, async artist =>
        {
            try
            {
                await FingerPrintMediaAsync(artist, album);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        });
    }
    
    public async Task FingerPrintMediaAsync(string artist, string album)
    {
        Console.WriteLine($"Processing artist '{artist}'");
        var metadatas = (await _metadataRepository.GetAllMetadataPathsByMissingFingerprintAsync(artist))
            .Where(metadata => string.IsNullOrWhiteSpace(album) || string.Equals(metadata.AlbumName, album, StringComparison.OrdinalIgnoreCase))
            .Where(metadata => new FileInfo(metadata.Path).Exists)
            .ToList();

        await ParallelHelper.ForEachAsync(metadatas, 4, async metadata =>
        {
            try
            {
                await FingerPrintFileAsync(metadata);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        });
    }

    private async Task FingerPrintFileAsync(MetadataModel metadata)
    {
        if (sw.Elapsed.Seconds >= 5)
        {
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] Generated FingerPrints: {generatedFingers}");
            sw.Restart();
        }
        
        FpcalcOutput? fingerprint = await _fingerPrintService.GetFingerprintAsync(metadata.Path);
        if (!string.IsNullOrWhiteSpace(fingerprint?.Fingerprint))
        {
            generatedFingers++;
            await _mediaTagWriteService.SaveTagAsync(new FileInfo(metadata.Path), "acoustid fingerprint", fingerprint.Fingerprint);
            FileInfo fileInfo = new FileInfo(metadata.Path);
            await _metadataRepository.UpdateMetadataFingerprintAsync(metadata.MetadataId.ToString(), fingerprint.Fingerprint, fingerprint.Duration, fileInfo.LastWriteTime, fileInfo.CreationTime);
        }
    }
}