using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class DeletedMediaCommandHandler
{
    private readonly MetadataRepository _metadataRepository;
    private readonly ArtistRepository _artistRepository;

    public DeletedMediaCommandHandler(string connectionString)
    {
        _metadataRepository = new MetadataRepository(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
    }

    public async Task CheckAllMissingTracksAsync(bool remove, string album)
    {
        foreach (var artist in await _artistRepository.GetAllArtistNamesAsync())
        {
            await CheckAllMissingTracksAsync(remove, artist, album);
        }
    }
    
    public async Task<int> CheckAllMissingTracksAsync(bool remove, string artist, string album)
    {
        var metadata = (await _metadataRepository.GetMetadataByArtistAsync(artist))
            .Where(metadata => string.IsNullOrWhiteSpace(album) || string.Equals(metadata.AlbumName, album, StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        int missingCount = 0;

        if (metadata.Count == 0)
        {
            return 0;
        }
        
        var missing = metadata
            .Where(metadata => !new FileInfo(metadata.Path).Exists)
            .ToList();
        
        missingCount += missing.Count;
        missing.ForEach(metadata => Console.WriteLine(metadata.Path));

        if (remove && missing.Count > 0)
        {
            await _metadataRepository.DeleteMetadataRecordsAsync(missing.Select(metadata => metadata.MetadataId.ToString()).ToList());
        }
        Console.WriteLine($"Total missing media files: {missingCount} for artist '{artist}'");
        return missingCount;
    }
}