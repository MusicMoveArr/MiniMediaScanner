using System.Collections.Concurrent;
using MiniMediaScanner.Repositories;

namespace MiniMediaScanner.Commands;

public class ImportWatcherCommandHandler : IDisposable
{
    private readonly string[] _ignoreExtensions = [ ".bak", ".tmp", ".temp" ];
    private readonly bool _forceImport;
    private readonly bool _updateMb;
    private ImportCommandHandler _importCommandHandler;
    private List<FileSystemWatcher> _watchers;
    private ConcurrentQueue<string> _queue;
    private ConcurrentQueue<RenamedEventArgs> _renameQueue;
    private readonly MetadataRepository _metadataRepository;

    public static string[] MediaFileExtensions = new string[]
    {
        "flac",
        "m4a",
        "wav",
        "aaif",
        "opus",
        "mp3",
    };

    public ImportWatcherCommandHandler(
        string connectionString, 
        int preventUpdateWithinDays = 0, 
        bool forceImport = false, 
        bool updateMb = false,
        bool splitArtists = false)
    {
        _queue = new ConcurrentQueue<string>();
        _renameQueue = new ConcurrentQueue<RenamedEventArgs>();
        _watchers = new List<FileSystemWatcher>();
        _importCommandHandler = new ImportCommandHandler(connectionString, preventUpdateWithinDays, forceImport, updateMb, splitArtists);
        _metadataRepository = new MetadataRepository(connectionString);
        _forceImport = forceImport;
        _updateMb = updateMb;
        ThreadPool.QueueUserWorkItem(WorkerThread);
    }
    
    public void WatchDirectory(string directoryPath)
    {
        Console.WriteLine($"Initializing watching {directoryPath}");
        FileSystemWatcher watcher = new FileSystemWatcher(directoryPath);
        watcher.Changed += WatcherOnChanged;
        watcher.Created += WatcherOnChanged;
        watcher.Deleted += WatcherOnChanged;
        watcher.Renamed += WatcherOnRenamed;
        
        watcher.InternalBufferSize = 1024 * 64;
        watcher.IncludeSubdirectories = true;
        watcher.EnableRaisingEvents = true;
        _watchers.Add(watcher);
        Console.WriteLine($"Watching {directoryPath}");
    }

    private void WatcherOnRenamed(object sender, RenamedEventArgs e)
    {
        if(MediaFileExtensions.Any(ext => e.FullPath.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) &&
           ((_ignoreExtensions.Any(ext => e.OldFullPath.Contains(ext)) &&
            !_ignoreExtensions.Any(ext => e.FullPath.Contains(ext))) ||
            (_ignoreExtensions.Any(ext => e.OldFullPath.Contains(ext)) &&
             _ignoreExtensions.Any(ext => e.FullPath.Contains(ext)))))
        {
            _renameQueue.Enqueue(e);
        }
    }

    private void WatcherOnChanged(object sender, FileSystemEventArgs e)
    {
        if(!_ignoreExtensions.Any(ext => e.FullPath.Contains(ext)) &&
           !_queue.Contains(e.FullPath) &&
           MediaFileExtensions.Any(ext => e.FullPath.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
        {
            _queue.Enqueue(e.FullPath);
        }
    }

    private void WorkerThread(object? o)
    {
        while (true)
        {
            try
            {
                while (_renameQueue.TryDequeue(out var eventArgs))
                {
                    Console.WriteLine($"Processing file renaming '{eventArgs.OldFullPath}' => '{eventArgs.Name}'");
                    if (!File.Exists(eventArgs.OldFullPath))
                    {
                        var metadataModels = _metadataRepository.GetMetadataByPathAsync(eventArgs.OldFullPath).Result;
                        if (metadataModels.Any())
                        {
                            _metadataRepository.DeleteMetadataRecordsAsync(metadataModels
                                .Select(model => model.MetadataId.ToString())
                                .ToList()).GetAwaiter().GetResult();
                        }
                    }

                    if (File.Exists(eventArgs.FullPath))
                    {
                        _importCommandHandler
                            .ProcessFileAsync(eventArgs.FullPath, _forceImport, _updateMb)
                            .GetAwaiter()
                            .GetResult();
                    }
                }

                while (_queue.TryDequeue(out var path))
                {
                    Console.WriteLine($"Processing '{path}'");

                    if (!File.Exists(path))
                    {
                        var metadataModels = _metadataRepository.GetMetadataByPathAsync(path).Result;
                        if (metadataModels.Any())
                        {
                            _metadataRepository.DeleteMetadataRecordsAsync(metadataModels
                                .Select(model => model.MetadataId.ToString())
                                .ToList()).GetAwaiter().GetResult();
                        }
                    }
                    else
                    {
                        _importCommandHandler
                            .ProcessFileAsync(path, _forceImport, _updateMb)
                            .GetAwaiter()
                            .GetResult();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\r\n" + e.StackTrace);
            }
            Thread.Sleep(TimeSpan.FromSeconds(10));
        }
    }

    public void Dispose()
    {
        foreach (var watcher in _watchers)
        {
            watcher.Dispose();
        }
    }
}