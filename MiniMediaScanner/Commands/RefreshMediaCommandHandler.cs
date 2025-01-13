using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class RefreshMetadataCommandHandler
{
    private readonly MetadataRepository _metadataRepository;
    private readonly ImportCommandHandler _importCommandHandler;
    private readonly ArtistRepository _artistRepository;

    public RefreshMetadataCommandHandler(string connectionString)
    {
        _metadataRepository = new MetadataRepository(connectionString);
        _importCommandHandler = new ImportCommandHandler(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
    }

    public void RefreshMetadata(string album)
    {
        _artistRepository.GetAllArtistNames()
            .ForEach(artist => RefreshMetadata(artist, album));
    }
    
    public void RefreshMetadata(string artist, string album)
    {
        var metadata = _metadataRepository.GetMetadataByArtist(artist)
            .Where(metadata => string.IsNullOrWhiteSpace(album) || string.Equals(metadata.AlbumName, album, StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        metadata
            .AsParallel()
            .WithDegreeOfParallelism(8)
            .ForAll(m => _importCommandHandler.ProcessFile(m.Path, true));
    }
}