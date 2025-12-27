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
    private const int BatchFileProcessing = 1000;
    private bool _importProcessing = false;
    private Channel<string> _processingChannel;
    private List<Task> _channelThreads;
    private bool _forceImport;
    private bool _updateMb;
    private ProgressTask _progressTask;
    private string _scanningDirectoryPath;

    public static string[] MediaFileExtensions = new string[]
    {
        "flac",
        "m4a",
        "wav",
        "aaif",
        "opus",
        "mp3",
    };

    public ImportCommandHandler(string connectionString, int preventUpdateWithinDays = 0, bool forceImport = false, bool updateMb = false)
    {
        _musicBrainzService = new MusicBrainzService(connectionString, preventUpdateWithinDays);
        _fileMetaDataService = new FileMetaDataService();
        _artistRepository = new ArtistRepository(connectionString);
        _metadataRepository =  new MetadataRepository(connectionString);
        _albumRepository =  new AlbumRepository(connectionString);
        _channelThreads = new List<Task>();
        _forceImport = forceImport;
        _updateMb = updateMb;
    }
    
    
    public async Task ProcessDirectoryAsync(string directoryPath)
    {
        int threads = _updateMb ? 1 : 8;
        int directoryIndex = 0;
        _scanningDirectoryPath = directoryPath;
        
        _importProcessing = true;
        _processingChannel = Channel.CreateBounded<string>(new BoundedChannelOptions(BatchFileProcessing * threads)
        {
            SingleWriter = false,
            SingleReader = false
        });

        for (int i = 0; i < threads; i++)
        {
            _channelThreads.Add(Task.Factory.StartNew(ChannelThread));
        }
        
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
                    _progressTask = ctx.AddTask("Scanning files");
                    _progressTask.MaxValue = 0;
                    
                    int fileIndex = 0;
                    while (true)
                    {
                        var chunkedFilePaths = Directory
                            .EnumerateFiles(directoryPath, "*.*", SearchOption.AllDirectories)
                            .Where(file => !Path.GetFileName(file).StartsWith("."))
                            .Skip(fileIndex)
                            .Take(BatchFileProcessing)
                            .ToList();

                        if (!chunkedFilePaths.Any())
                        {
                            break;
                        }

                        fileIndex += BatchFileProcessing;

                        chunkedFilePaths = chunkedFilePaths
                            .Where(file => MediaFileExtensions.Any(ext => file.EndsWith(ext)))
                            .ToList();

                        _progressTask.MaxValue += chunkedFilePaths.Count;
                        _progressTask.Description = $"Scanning files from '{_scanningDirectoryPath}' {_progressTask.Value}/{_progressTask.MaxValue}";

                        foreach (var path in chunkedFilePaths)
                        {
                            await _processingChannel.Writer.WriteAsync(path);
                        }
                    }
                });
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }
        
        _importProcessing = false;
        await Task.WhenAll(_channelThreads);
    }

    private async Task ChannelThread()
    {
        List<string> filePaths = new List<string>();
        List<string> updatePaths = new List<string>();
        while (_importProcessing)
        {
            await foreach (var path in _processingChannel.Reader.ReadAllAsync())
            {
                filePaths.Add(path);
                if (filePaths.Count == BatchFileProcessing)
                {
                    updatePaths = _forceImport ? filePaths : await _metadataRepository.MetadataCanUpdatePathListAsync(filePaths);
                    foreach (var file in updatePaths)
                    {
                        await ProcessFileAsync(file, true, _updateMb);
                    }

                    filePaths.Clear();
                }
            }
            
            Thread.Sleep(TimeSpan.FromMilliseconds(1));
        }

        await foreach (var path in _processingChannel.Reader.ReadAllAsync())
        {
            filePaths.Add(path);
        }

        if (filePaths.Count > 0)
        {
            updatePaths = _forceImport ? filePaths : await _metadataRepository.MetadataCanUpdatePathListAsync(filePaths);
            foreach (var file in updatePaths)
            {
                await ProcessFileAsync(file, true, _updateMb);
            }
            filePaths.Clear();
        }
    }

    public async Task<bool> ProcessFileAsync(string filePath, bool forceReimport = false, bool updateMb = false)
    {
        _progressTask.Value++;
        _progressTask.Description = $"Scanning files from '{_scanningDirectoryPath}' {_progressTask.Value}/{_progressTask.MaxValue}";
        
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