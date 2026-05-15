using System.Diagnostics;
using System.Threading.Channels;
using MiniMediaScanner.Enums;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models;
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
    private readonly AsyncLock _asyncMbLock = new AsyncLock();
    private readonly AsyncLock _asyncArtistLock = new AsyncLock();
    private const int BatchFileProcessing = 1000;
    private bool _importProcessing = false;
    private Channel<string> _processingChannel;
    private readonly List<Task> _channelThreads;
    private readonly bool _forceImport;
    private readonly bool _updateMb;
    private ProgressTask _progressTask;
    private string _scanningDirectoryPath;
    private readonly bool _splitArtists;

    public static string[] MediaFileExtensions = new string[]
    {
        "flac",
        "m4a",
        "wav",
        "aaif",
        "opus",
        "mp3",
    };

    public ImportCommandHandler(
        string connectionString, 
        int preventUpdateWithinDays = 0, 
        bool forceImport = false, 
        bool updateMb = false,
        bool splitArtists = false)
    {
        _musicBrainzService = new MusicBrainzService(connectionString, preventUpdateWithinDays);
        _fileMetaDataService = new FileMetaDataService();
        _artistRepository = new ArtistRepository(connectionString);
        _metadataRepository =  new MetadataRepository(connectionString);
        _albumRepository =  new AlbumRepository(connectionString);
        _channelThreads = new List<Task>();
        _forceImport = forceImport;
        _updateMb = updateMb;
        _splitArtists = splitArtists;
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
            _channelThreads.Add(Task.Factory.StartNew(ChannelThread).Unwrap());
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

        _processingChannel.Writer.TryComplete();
        _importProcessing = false;
        await Task.WhenAll(_channelThreads);
    }

    private async Task ChannelThread()
    {
        List<string> filePaths = new List<string>();
        while (_importProcessing)
        {
            await foreach (var path in _processingChannel.Reader.ReadAllAsync())
            {
                filePaths.Add(path);
                if (filePaths.Count == BatchFileProcessing)
                {
                    var updatePaths = _forceImport ? filePaths : await _metadataRepository.MetadataCanUpdatePathListAsync(filePaths);
                    var notUpdatedPaths = filePaths.Except(updatePaths);
                    foreach (var file in updatePaths)
                    {
                        _progressTask.Value++;
                        _progressTask.Description = $"Scanning files from '{_scanningDirectoryPath}' {_progressTask.Value}/{_progressTask.MaxValue}";
                        await ProcessFileAsync(file, _forceImport, _updateMb);
                    }
                    foreach (var file in notUpdatedPaths)
                    {
                        _progressTask.Value++;
                        _progressTask.Description = $"Scanning files from '{_scanningDirectoryPath}' {_progressTask.Value}/{_progressTask.MaxValue}";
                        FileInfo fileInfo = new FileInfo(file);
                        await _metadataRepository.UpdateFileSizeWhenNotSetAsync(file, fileInfo.Length);
                    }
                    
                    _progressTask.Value += filePaths.Count - updatePaths.Count;
                    _progressTask.Description = $"Scanning files from '{_scanningDirectoryPath}' {_progressTask.Value}/{_progressTask.MaxValue}";

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
            var updatePaths = _forceImport ? filePaths : await _metadataRepository.MetadataCanUpdatePathListAsync(filePaths);
            var notUpdatedPaths = filePaths.Except(updatePaths);
            foreach (var file in updatePaths)
            {
                _progressTask.Value++;
                _progressTask.Description = $"Scanning files from '{_scanningDirectoryPath}' {_progressTask.Value}/{_progressTask.MaxValue}";
                await ProcessFileAsync(file, _forceImport, _updateMb);
            }
            foreach (var file in notUpdatedPaths)
            {
                _progressTask.Value++;
                _progressTask.Description = $"Scanning files from '{_scanningDirectoryPath}' {_progressTask.Value}/{_progressTask.MaxValue}";
                FileInfo fileInfo = new FileInfo(file);
                await _metadataRepository.UpdateFileSizeWhenNotSetAsync(file, fileInfo.Length);
            }
            filePaths.Clear();
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
                Console.WriteLine($"{e.Message}, {e.StackTrace}");
                return false;
            }
            
            await ProcessMetadataAsync(metadata);
                
            if (updateMb &&
                !string.IsNullOrWhiteSpace(metadata?.MusicBrainzArtistId))
            {
                using (await _asyncMbLock.LockAsync())
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
        Guid artistId = Guid.Empty;
        using (await _asyncMbLock.LockAsync())
        {
            artistId = _splitArtists ? await GetSplitArtistAsync(metadata) 
                : await _artistRepository.InsertOrFindArtistAsync(metadata.Artist);
        }

        // 2. Insert/Find Album
        var albumId = await _albumRepository.InsertOrFindAlbumAsync(metadata.Album, artistId);

        // 3. Insert/Update Metadata
        await _metadataRepository.UpsertMetadataAsync(metadata, albumId);
    }

    private async Task<Guid> GetSplitArtistAsync(MetadataInfo metadata)
    {
        List<ArtistExtModel> artistExtModels = new List<ArtistExtModel>();
        string mbArtistId = metadata.MusicBrainzArtistId
            ?.Split(';', StringSplitOptions.RemoveEmptyEntries)
            ?.FirstOrDefault() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(metadata.MusicBrainzArtistId))
        {
            artistExtModels.Add(new ArtistExtModel(Guid.Empty, mbArtistId, nameof(ProviderName.MusicBrainz)));
        }
        if (!string.IsNullOrWhiteSpace(metadata.SpotifyArtistId))
        {
            artistExtModels.Add(new ArtistExtModel(Guid.Empty, metadata.SpotifyArtistId, nameof(ProviderName.Spotify)));
        }
        if (metadata.DeezerArtistId > 0)
        {
            artistExtModels.Add(new ArtistExtModel(Guid.Empty, metadata.DeezerArtistId.ToString(), nameof(ProviderName.Deezer)));
        }
        if (metadata.DiscogsArtistId > 0)
        {
            artistExtModels.Add(new ArtistExtModel(Guid.Empty, metadata.DiscogsArtistId.ToString(), nameof(ProviderName.Discogs)));
        }
        if (metadata.SoundcloudArtistId > 0)
        {
            artistExtModels.Add(new ArtistExtModel(Guid.Empty, metadata.SoundcloudArtistId.ToString(), nameof(ProviderName.Soundcloud)));
        }
        if (metadata.TidalArtistId > 0)
        {
            artistExtModels.Add(new ArtistExtModel(Guid.Empty, metadata.TidalArtistId.ToString(), nameof(ProviderName.Tidal)));
        }

        string artistName = string.Empty;
        if (!metadata.MediaTags.TryGetValue("artist", out artistName))
        {
            metadata.MediaTags.TryGetValue("album_artist", out artistName);
        }

        if (string.IsNullOrWhiteSpace(artistName))
        {
            artistName = "[unknown]";
        }

        Guid artistId = Guid.Empty;
        List<ArtistModel> artists = new List<ArtistModel>();

        using (await _asyncArtistLock.LockAsync())
        {
            artists.AddRange(await _artistRepository.GetMatchingArtistAsync(artistName));
        
            if (!artists.Any())
            {
                artistId = await _artistRepository.InsertArtistAsync(artistName);
                artistExtModels.ForEach(ext => ext.ArtistId = artistId);
                await _artistRepository.BulkInsertArtistExtAsync(artistExtModels);
                return artistId;
            }
        }

        
        //find artist with maching artist id from different providers
        var artist = artists
            .FirstOrDefault(artist => artist.ExtArtists
                .Any(ext => (ext.Provider == nameof(ProviderName.MusicBrainz) && ext.ExtArtistId.StartsWith(mbArtistId, StringComparison.OrdinalIgnoreCase)) ||
                            (ext.Provider == nameof(ProviderName.Spotify) && string.Equals(ext.ExtArtistId, metadata.SpotifyArtistId)) ||
                            (ext.Provider == nameof(ProviderName.Deezer) && string.Equals(ext.ExtArtistId, metadata.DeezerArtistId.ToString())) ||
                            (ext.Provider == nameof(ProviderName.Discogs) && string.Equals(ext.ExtArtistId, metadata.DiscogsArtistId.ToString())) ||
                            (ext.Provider == nameof(ProviderName.Soundcloud) && string.Equals(ext.ExtArtistId, metadata.SoundcloudArtistId.ToString())) ||
                            (ext.Provider == nameof(ProviderName.Tidal) && string.Equals(ext.ExtArtistId, metadata.TidalArtistId.ToString()))))
                     ?? artists.FirstOrDefault(artist => !artist.ExtArtists.Any());
        
        //if no match found, grab first artist (best matched name)
        if (artist == null && !artistExtModels.Any())
        {
            artist = artists.FirstOrDefault();
        }

        using (await _asyncArtistLock.LockAsync())
        {
            artistId = artist?.ArtistId ?? await _artistRepository.InsertArtistAsync(artistName);
        }
        
        var insertExtModels = artist?.ExtArtists != null ? artistExtModels
            .ExceptBy(artist.ExtArtists.Select(ext => ext.Provider), ext => ext.Provider)
            .ToList() : artistExtModels;

        if (insertExtModels.Any())
        {
            insertExtModels.ForEach(ext => ext.ArtistId = artistId);
            await _artistRepository.BulkInsertArtistExtAsync(insertExtModels);
        }

        return artistId;
    }
}