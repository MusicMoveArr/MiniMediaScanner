using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class DeletedMediaCommandHandler
{
    private readonly MetadataRepository _metadataRepository;

    public DeletedMediaCommandHandler(string connectionString)
    {
        _metadataRepository = new MetadataRepository(connectionString);
    }
    
    public void CheckAllMissingTracks(bool remove)
    {
        int missingCount = 0;
        const int limit = 1000;
        int offset = 0;
        while (true)
        {
            var metadata = _metadataRepository.GetAllMetadata(offset, limit);
            offset += limit;

            if (metadata.Count == 0)
            {
                break;
            }
            
            var missing = metadata
                .Where(metadata => !new FileInfo(metadata.Path).Exists)
                .ToList();
            
            missingCount += missing.Count;
            missing.ForEach(metadata => Console.WriteLine(metadata.Path));

            if (remove && missing.Count > 0)
            {
                _metadataRepository.DeleteMetadataRecords(missing.Select(metadata => metadata.MetadataId).ToList());
            }
        }
        Console.WriteLine($"Total missing media files: {missingCount}");
    }
}