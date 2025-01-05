using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class ImportCommandHandler
{
    private readonly DatabaseService _databaseService;
    private readonly MusicBrainzService _musicBrainzService;
    private readonly FileMetaDataService _fileMetaDataService;

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
        _databaseService = new DatabaseService(connectionString);
        _musicBrainzService = new MusicBrainzService(connectionString);
        _fileMetaDataService = new FileMetaDataService();
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

            if (!_databaseService.MetadataCanUpdate(fileInfo.FullName, fileInfo.LastWriteTime, fileInfo.CreationTime))
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
                _musicBrainzService.InsertMissingMusicBrainzArtist(metadata);
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
        var artistId = _databaseService.InsertOrFindArtist(metadata.Artist);

        // 2. Insert/Find Album
        var albumId = _databaseService.InsertOrFindAlbum(metadata.Album, artistId);

        // 3. Insert/Update Metadata
        _databaseService.InsertOrUpdateMetadata(metadata, filePath, albumId);
    }
}