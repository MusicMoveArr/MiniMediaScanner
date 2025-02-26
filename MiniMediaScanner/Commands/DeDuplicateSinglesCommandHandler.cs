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

    public void CheckDuplicateFiles(bool delete)
    {
        _artistRepository.GetAllArtistNames()
            .ForEach(artist => CheckDuplicateFiles(artist, delete));
    }

    public void CheckDuplicateFiles(string artistName, bool delete)
    {
        FindDuplicateSingles(artistName, delete);
    }

    private void FindDuplicateSingles(string artistName, bool delete)
    {
        var metadata = _metadataRepository.GetMetadataByArtist(artistName);
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