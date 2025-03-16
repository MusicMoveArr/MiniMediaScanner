using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class RefreshMetadataCommandHandler
{
    private readonly MetadataRepository _metadataRepository;
    private readonly ImportCommandHandler _importCommandHandler;
    private readonly ArtistRepository _artistRepository;
    private readonly MetadataTagRepository _metadataTagRepository;

    public RefreshMetadataCommandHandler(string connectionString)
    {
        _metadataRepository = new MetadataRepository(connectionString);
        _importCommandHandler = new ImportCommandHandler(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
        _metadataTagRepository =  new MetadataTagRepository(connectionString);
    }

    public async Task RefreshMetadataAsync(string album)
    {
        foreach (var artist in await _artistRepository.GetAllArtistNamesAsync())
        {
            await RefreshMetadataAsync(artist, album);
        }
    }
    
    public async Task RefreshMetadataAsync(string artist, string album)
    {
        try
        {
            Console.WriteLine($"Processing {artist}");
            var metadatas = (await _metadataRepository.GetMetadataByArtistAsync(artist))
                .Where(metadata => string.IsNullOrWhiteSpace(album) || string.Equals(metadata.AlbumName, album, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var metadata in metadatas
                         .AsParallel()
                         .WithDegreeOfParallelism(4))
            {
                await _importCommandHandler.ProcessFileAsync(metadata.Path, true);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}