using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class RefreshMetadataCommandHandler
{
    private readonly MetadataRepository _metadataRepository;
    private readonly ImportCommandHandler _importCommandHandler;

    public RefreshMetadataCommandHandler(string connectionString)
    {
        _metadataRepository = new MetadataRepository(connectionString);
        _importCommandHandler = new ImportCommandHandler(connectionString);
    }
    
    public void RefreshMetadata()
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

            metadata
                .AsParallel()
                .WithDegreeOfParallelism(8)
                .ForAll(m => _importCommandHandler.ProcessFile(m.Path, true));
                
        }
        Console.WriteLine($"Total missing media files: {missingCount}");
    }
}