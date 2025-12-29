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
    private readonly List<Task> _channelThreads;
    private readonly bool _forceImport;
    private readonly bool _updateMb;
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

                    int scannedFiles = 0;
                    Stack<string> directoryStack = new Stack<string>();
                    directoryStack.Push(directoryPath);
                    while (directoryStack.Count > 0)
                    {
                        string path = directoryStack.Pop();
                        foreach (string subDir in Directory.GetDirectories(path))
                        {
                            directoryStack.Push(subDir);
                        }

                        try
                        {
                            string[] files = Directory.GetFiles(path);
                            
                            files = files
                                .Where(file => MediaFileExtensions.Any(ext => file.EndsWith(ext)))
                                .ToArray();
                            _progressTask.MaxValue += files.Length;
                            _progressTask.Description = $"Scanning files from '{_scanningDirectoryPath}' {_progressTask.Value}/{_progressTask.MaxValue}";

                            scannedFiles += files.Length;
                        
                            foreach (var filepath in files)
                            {
                                await _processingChannel.Writer.WriteAsync(filepath);

                                //waiting a little before fetching more data
                                while (_processingChannel.Reader.Count > BatchFileProcessing * threads)
                                {
                                    Thread.Sleep(TimeSpan.FromMilliseconds(1000));
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"{e.Message}, {e.StackTrace}");
                        }
                    }
                });
        }
        catch (Exception e)
        {
            Console.WriteLine($"{e.Message}, {e.StackTrace}");
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
                        await ProcessFileAsync(file, _forceImport, _updateMb);
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
                await ProcessFileAsync(file, _forceImport, _updateMb);
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
                Console.WriteLine($"{e.Message}, {e.StackTrace}");
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
            Console.WriteLine($"{e.Message}, {e.StackTrace}");
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