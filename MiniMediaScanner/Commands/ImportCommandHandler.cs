using System.Diagnostics;
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

    public static string[] MediaFileExtensions = new string[]
    {
        "flac",
        "m4a",
        "wav",
        "aaif",
        "opus",
        "mp3",
    };

    public ImportCommandHandler(string connectionString)
    {
        _musicBrainzService = new MusicBrainzService(connectionString);
        _fileMetaDataService = new FileMetaDataService();
        _artistRepository = new ArtistRepository(connectionString);
        _metadataRepository =  new MetadataRepository(connectionString);
        _albumRepository =  new AlbumRepository(connectionString);
    }
    
    public async Task ProcessDirectoryAsync(string directoryPath, bool updateMb)
    {
        try
        {
            var sortedTopDirectories = Directory
                .EnumerateFileSystemEntries(directoryPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(dir => !new DirectoryInfo(dir).Name.StartsWith("."))
                .OrderBy(dir => dir)
                .ToList();

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
                await ParallelHelper.ForEachAsync(sortedTopDirectories, threads, async dir =>
                {
                    var task = ctx.AddTask(Markup.Escape($"Importing {dir}"));

                    var allFilePaths = Directory
                        .EnumerateFileSystemEntries(dir, "*.*", SearchOption.AllDirectories)
                        .Where(file => !new DirectoryInfo(file).Name.StartsWith("."))
                        .ToList();

                    task.MaxValue = allFilePaths.Count;
                    
                    foreach (var file in allFilePaths)
                    {
                        await ProcessFileAsync(file, false, updateMb);
                        task.Value++;
                    }
                });
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
                metadata = _fileMetaDataService.GetMetadataInfo(fileInfo);
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