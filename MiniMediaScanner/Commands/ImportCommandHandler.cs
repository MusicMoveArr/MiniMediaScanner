using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class ImportCommandHandler
{
    private readonly MusicBrainzService _musicBrainzService;
    private readonly FileMetaDataService _fileMetaDataService;
    private readonly ArtistRepository _artistRepository;
    private readonly MetadataRepository _metadataRepository;
    private readonly AlbumRepository _albumRepository;

    private static string[] MediaFileExtensions = new string[]
    {
        "flac",
        "mp3",
        "m4a",
        "wav",
        "aaif",
        "opus"
    };

    public ImportCommandHandler(string connectionString)
    {
        _musicBrainzService = new MusicBrainzService(connectionString);
        _fileMetaDataService = new FileMetaDataService();
        _artistRepository = new ArtistRepository(connectionString);
        _metadataRepository =  new MetadataRepository(connectionString);
        _albumRepository =  new AlbumRepository(connectionString);
    }
    
    public void ProcessDirectory(string directoryPath)
    {
        try
        {
            foreach (var file in Directory.EnumerateFileSystemEntries(directoryPath, "*.*", SearchOption.AllDirectories))
            {
                ProcessFile(file);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public bool ProcessFile(string filePath)
    {
        try
        {
            if (!MediaFileExtensions.Any(ext => filePath.EndsWith(ext)))
            {
                return false;
            }
                    
            FileInfo fileInfo = new(filePath);

            if (!_metadataRepository.MetadataCanUpdate(fileInfo.FullName, fileInfo.LastWriteTime, fileInfo.CreationTime))
            {
                return false;
            }
                    
            Console.WriteLine($"Scanning file {filePath}");

            var metadata = default(MetadataInfo);

            try
            {
                metadata = _fileMetaDataService.GetMetadataInfo(fileInfo);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
                        
            ProcessMetadata(metadata, filePath);
                
            if (!string.IsNullOrWhiteSpace(metadata?.MusicBrainzTrackId) &&
                !string.IsNullOrWhiteSpace(metadata?.Album) &&
                !string.IsNullOrWhiteSpace(metadata?.Artist))
            {
                lock (_musicBrainzService)
                {
                    _musicBrainzService.InsertMissingMusicBrainzArtist(metadata);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return true;
    }
    
    private void ProcessMetadata(MetadataInfo metadata, string filePath)
    {
        // 1. Insert/Find Artist
        var artistId = _artistRepository.InsertOrFindArtist(metadata.Artist);

        // 2. Insert/Find Album
        var albumId = _albumRepository.InsertOrFindAlbum(metadata.Album, artistId);

        // 3. Insert/Update Metadata
        _metadataRepository.InsertOrUpdateMetadata(metadata, filePath, albumId);
    }
}