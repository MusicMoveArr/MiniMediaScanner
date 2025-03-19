using MiniMediaScanner.Helpers;
using MiniMediaScanner.Repositories;

namespace MiniMediaScanner.Commands;

public class DeDuplicateSinglesCommandHandler
{
    private readonly ArtistRepository _artistRepository;
    private readonly MetadataRepository _metadataRepository;

    public static int CleanupCount = 0;

    public DeDuplicateSinglesCommandHandler(string connectionString)
    {
        _artistRepository = new ArtistRepository(connectionString);
        _metadataRepository = new MetadataRepository(connectionString);
    }

    public async Task CheckDuplicateFilesAsync(bool delete)
    {
        await ParallelHelper.ForEachAsync(await _artistRepository.GetAllArtistNamesAsync(), 4, async artist =>
        {
            try
            {
                await CheckDuplicateFilesAsync(artist, delete);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        });
    }

    public async Task CheckDuplicateFilesAsync(string artistName, bool delete)
    {
        await FindDuplicateSinglesAsync(artistName, delete);
    }

    private async Task FindDuplicateSinglesAsync(string artistName, bool delete)
    {
        var metadata = await _metadataRepository.GetMetadataByArtistAsync(artistName);
        var singleAlbums = metadata
            .GroupBy(group => new { group.AlbumId })
            .Where(group => group.Count() == 1);

        int prevCleanupCount = CleanupCount;

        foreach (var singleAlbum in singleAlbums)
        {
            var singleSong = singleAlbum.First();
            var similarSongs = metadata
                .Where(m => !string.Equals(m.AlbumId, singleSong.AlbumId))
                .Where(m => string.Equals(m.Title, singleSong.Title))
                .Where(m => string.Equals(m.Tag_AcoustId, singleSong.Tag_AcoustId))
                .Where(m => !string.Equals(m.Path, singleSong.Path))
                .ToList();

            if (similarSongs.Count == 0)
            {
                continue;
            }
            
            Console.WriteLine($"Single Track: {singleSong.Title}, Album: {singleSong.AlbumName}, Artist: {singleSong.ArtistName}, Path: {singleSong.Path}");
            foreach (var similarSong in similarSongs)
            {
                Console.WriteLine($"Similar tracks: {similarSong.Title}, Album: {similarSong.AlbumName}, Artist: {similarSong.ArtistName}, Path: {similarSong.Path}");
            }
            
            Console.WriteLine();
            CleanupCount++;
            
        }

        if (prevCleanupCount != CleanupCount)
        {
            Console.WriteLine($"Cleanup: {CleanupCount}");
        }
    }
}