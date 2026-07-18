using System.Diagnostics;
using ATL;
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
    private readonly FileMetaDataService _fileMetaDataService;

    private Stopwatch sw = Stopwatch.StartNew();
    private int generatedFingers = 0;
    public FingerPrintMediaCommandHandler(string connectionString)
    {
        _fingerPrintService = new FingerPrintService();
        _metadataRepository = new MetadataRepository(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
        _mediaTagWriteService = new MediaTagWriteService();
        _fileMetaDataService = new FileMetaDataService();
    }
    
    public async Task FingerPrintMediaAsync(string album)
    {
        await ParallelHelper.ForEachAsync(await _artistRepository.GetAllArtistNamesLowercaseUniqueAsync(), 4, async artist =>
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

        if (!string.IsNullOrWhiteSpace(metadata.Tag_AcoustIdFingerprint) &&
            metadata.Tag_AcoustIdFingerprintDuration > 0)
        {
            await SaveFingerprintAsync(metadata.MetadataId.ToString(), metadata.Path, metadata.Tag_AcoustIdFingerprint,  metadata.Tag_AcoustIdFingerprintDuration);
        }
        else
        {
            FpcalcOutput? fingerprint = await _fingerPrintService.GetFingerprintAsync(metadata.Path);
            if (!string.IsNullOrWhiteSpace(fingerprint?.Fingerprint))
            {
                await SaveFingerprintAsync(metadata.MetadataId.ToString(), metadata.Path, fingerprint.Fingerprint, fingerprint.Duration);
            }
        }
    }

    private async Task SaveFingerprintAsync(string metadataId, string path, string fingerprint, float duration)
    {
        generatedFingers++;
            
        Track track = new Track(path);
        var metadataInfo = _fileMetaDataService.GetMetadataInfo(track);
        bool trackInfoUpdated = false;
            
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "acoustid fingerprint", fingerprint, ref trackInfoUpdated, true);
        _mediaTagWriteService.UpdateTag(track, metadataInfo, "acoustid fingerprint duration", duration.ToString(), ref trackInfoUpdated, true);
            
        await _mediaTagWriteService.SafeSaveAsync(track);
            
        FileInfo fileInfo = new FileInfo(path);
        await _metadataRepository.UpdateMetadataFingerprintAsync(metadataId.ToString(), fingerprint, duration, fileInfo.LastWriteTime, fileInfo.CreationTime);

    }
}