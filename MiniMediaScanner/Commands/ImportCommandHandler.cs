using System.Diagnostics;
using System.Threading.Channels;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;
using Spectre.Console;

namespace MiniMediaScanner.Commands;

public class ImportCommandHandler
{
    private readonly MusicBrainzService _musicBrainzService;
    private readonly FileMetaDataService _fileMetaDataService;
    private readonly ArtistRepository _artistRepository;
    private readonly MetadataRepository _metadataRepository;
    private readonly AlbumRepository _albumRepository;
    private readonly AsyncLock _asyncLock = new AsyncLock();
    private const int BatchFileProcessing = 100;

    public static string[] MediaFileExtensions = new string[]
    {
        "flac",
        "m4a",
        "wav",
        "aaif",
        "opus",
        "mp3",
    };

    public ImportCommandHandler(string connectionString, int preventUpdateWithinDays = 0)
    {
        _musicBrainzService = new MusicBrainzService(connectionString, preventUpdateWithinDays);
        _fileMetaDataService = new FileMetaDataService();
        _artistRepository = new ArtistRepository(connectionString);
        _metadataRepository =  new MetadataRepository(connectionString);
        _albumRepository =  new AlbumRepository(connectionString);
    }
    
    
    public async Task ProcessDirectoryAsync(string directoryPath, bool forceImport, bool updateMb)
    {
        try
        {
            await AnsiConsole.Progress()
                .HideCompleted(true)
                .AutoClear(true)
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn()
                    {
                        Alignment = Justify.Left
                    },
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new RemainingTimeColumn(),
                })
                .StartAsync(async ctx =>
                {
                    int threads = updateMb ? 1 : 8;
                    var totalTask = ctx.AddTask(Markup.Escape($"Scanning directories {directoryPath}"));
                    totalTask.MaxValue = 0;
                    int fileChunkSize = 100;
                    
                    int directoryChunkSize = 100;
                    int directoryIndex = 0;

                    while (true)
                    {
                        var chunkedDirPaths = Directory
                            .EnumerateDirectories(directoryPath, "*.*", SearchOption.AllDirectories)
                            .Skip(directoryIndex)
                            .Take(directoryChunkSize)
                            .ToList();
                        directoryIndex += directoryChunkSize;

                        if (!chunkedDirPaths.Any())
                        {
                            break;
                        }

                        totalTask.MaxValue += chunkedDirPaths.Count;
                        
                        await ParallelHelper.ForEachAsync(chunkedDirPaths, threads, async dir =>
                        {
                            totalTask.Value++;
                            totalTask.Description = $"Scanning directories {totalTask.Value}/{totalTask.MaxValue}";
                            var fileScanTask = ctx.AddTask(Markup.Escape($"Scanning directory '{dir}'"));
                            fileScanTask.MaxValue = 0;
                            
                            int fileIndex = 0;
                            while (true)
                            {
                                var chunkedFilePaths = Directory
                                    .EnumerateFiles(dir, "*.*", SearchOption.AllDirectories)
                                    .Where(file => !Path.GetFileName(file).StartsWith("."))
                                    .Skip(fileIndex)
                                    .Take(fileChunkSize)
                                    .ToList();

                                fileIndex += fileChunkSize;

                                chunkedFilePaths = chunkedFilePaths
                                    .Where(file => MediaFileExtensions.Any(ext => file.EndsWith(ext)))
                                    .ToList();

                                if (!chunkedFilePaths.Any())
                                {
                                    return;
                                }
                        
                                List<string> updatePaths = forceImport ? chunkedFilePaths : await _metadataRepository.MetadataCanUpdatePathListAsync(chunkedFilePaths);
                                fileScanTask.MaxValue += updatePaths.Count;
                                
                                foreach (var file in updatePaths)
                                {
                                    await ProcessFileAsync(file, true, updateMb);
                                    fileScanTask.Value++;
                                }
                            }
                            
                        });
                    }
                });
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }
    }
    
    

    public async Task<bool> ProcessFileAsync(string filePath, bool forceReimport = false, bool updateMb = false)
    {
        var metadata = default(MetadataInfo);

        try
        {
            if (!MediaFileExtensions.Any(ext => filePath.EndsWith(ext)))
            {
                return false;
            }
                    
            FileInfo fileInfo = new(filePath);

            if (!fileInfo.Exists)
            {
                return false;
            }

            if (!forceReimport && 
                !await _metadataRepository.MetadataCanUpdateAsync(fileInfo.FullName, fileInfo.LastWriteTime, fileInfo.CreationTime))
            {
                return false;
            }
            Debug.WriteLine($"Scanning {fileInfo.FullName}");

            try
            {
                metadata = await _fileMetaDataService.GetMetadataInfoAsync(fileInfo);
                metadata.NonNullableValues();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return false;
            }
            
            await ProcessMetadataAsync(metadata);
                
            if (updateMb &&
                !string.IsNullOrWhiteSpace(metadata?.MusicBrainzArtistId))
            {
                using (await _asyncLock.LockAsync())
                {
                    await _musicBrainzService.InsertMissingMusicBrainzArtistAsync(metadata);
                }
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }

        return true;
    }
    
    private async Task ProcessMetadataAsync(MetadataInfo metadata)
    {
        // 1. Insert/Find Artist
        var artistId = await _artistRepository.InsertOrFindArtist(metadata.Artist);

        // 2. Insert/Find Album
        var albumId = await _albumRepository.InsertOrFindAlbumAsync(metadata.Album, artistId);

        // 3. Insert/Update Metadata
        await _metadataRepository.InsertOrUpdateMetadataAsync(metadata, albumId);
    }
}