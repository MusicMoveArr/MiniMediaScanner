using MiniMediaScanner.Helpers;
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

    public async Task RefreshMetadataAsync(string album)
    {
        await ParallelHelper.ForEachAsync(await _artistRepository.GetAllArtistNamesAsync(), 4, async artist =>
        {
            try
            {
                await RefreshMetadataAsync(artist, album);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        });
    }
    
    public async Task RefreshMetadataAsync(string artist, string album)
    {
        try
        {
            Console.WriteLine($"Processing {artist}");
            var metadatas = (await _metadataRepository.GetMetadataByArtistAsync(artist))
                .Where(metadata => string.IsNullOrWhiteSpace(album) || string.Equals(metadata.AlbumName, album, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var metadata in metadatas)
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