using System.Diagnostics;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class ImportCommandHandler
{
    private readonly MusicBrainzService _musicBrainzService;
    private readonly FileMetaDataService _fileMetaDataService;
    private readonly ArtistRepository _artistRepository;
    private readonly MetadataRepository _metadataRepository;
    private readonly MetadataTagRepository _metadataTagRepository;
    private readonly AlbumRepository _albumRepository;

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
        _metadataTagRepository = new MetadataTagRepository(connectionString);
    }
    
    public async Task ProcessDirectoryAsync(string directoryPath)
    {
        try
        {
            foreach (var file in Directory.EnumerateFileSystemEntries(directoryPath, "*.*", SearchOption.AllDirectories))
            {
                await ProcessFileAsync(file);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public async Task<bool> ProcessFileAsync(string filePath, bool forceReimport = false)
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

            Console.WriteLine($"Scanning {fileInfo.FullName}");

            try
            {
                metadata = _fileMetaDataService.GetMetadataInfo(fileInfo);
                metadata.NonNullableValues();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
            
            await ProcessMetadataAsync(metadata);
                
            if (!string.IsNullOrWhiteSpace(metadata?.MusicBrainzArtistId) &&
                !string.IsNullOrWhiteSpace(metadata?.Album) &&
                !string.IsNullOrWhiteSpace(metadata?.Artist))
            {
                await _musicBrainzService.InsertMissingMusicBrainzArtistAsync(metadata);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
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
        await _metadataTagRepository.InsertOrUpdateMetadataTagAsync(metadata);
    }
}