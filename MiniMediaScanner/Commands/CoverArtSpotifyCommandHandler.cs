using MiniMediaScanner.Models;
using MiniMediaScanner.Repositories;
using RestSharp;

namespace MiniMediaScanner.Commands;

public class CoverArtSpotifyCommandHandler
{
    private readonly MetadataRepository _metadataRepository;
    private readonly ArtistRepository _artistRepository;
    private readonly MatchRepository _matchRepository;
    private readonly SpotifyRepository _spotifyRepository;

    public CoverArtSpotifyCommandHandler(string connectionString)
    {
        _metadataRepository = new MetadataRepository(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
        _matchRepository = new MatchRepository(connectionString);
        _spotifyRepository = new SpotifyRepository(connectionString);
    }

    public void CheckAllMissingCovers(string album, string coverAlbumFileName, string coverArtistFileName)
    {
        _artistRepository.GetAllArtistNames()
            .AsParallel()
            .WithDegreeOfParallelism(4)
            .ForAll(artist => CheckAllMissingCovers(artist, album, coverAlbumFileName, coverArtistFileName));
    }
    
    public void CheckAllMissingCovers(string artist, string album, string coverAlbumFileName, string coverArtistFileName)
    {
        Console.WriteLine($"Checking artist '{artist}'");
        var coverModels = _metadataRepository.GetFolderPathsByArtistForCovers(artist, album)
            .ToList();
        
        Dictionary<Guid, string> spotifyArtistIds = coverModels
            .DistinctBy(cover => cover.ArtistId)
            .Select(cover => new
            {
                SpotifyArtistId = _matchRepository.GetBestSpotifyMatch(cover.ArtistId, cover.ArtistName),
                CoverArtistId = cover.ArtistId,
            })
            .Where(cover => !string.IsNullOrWhiteSpace(cover.SpotifyArtistId))
            .ToDictionary(key => key.CoverArtistId, key => key.SpotifyArtistId!);

        if (spotifyArtistIds.Count == 0)
        {
            Console.WriteLine($"No spotify artist found in the database for '{artist}'");
            return;
        }

        foreach (MetadataPathCoverModel coverModel in coverModels)
        {
            CheckAlbumCover(coverModel, coverAlbumFileName, spotifyArtistIds);
        }

        CheckArtistCover(coverModels, coverArtistFileName, spotifyArtistIds);
    }

    private void CheckArtistCover(List<MetadataPathCoverModel> coverModels, 
        string coverArtistFileName,
        Dictionary<Guid, string> spotifyArtistIds)
    {
        var coverModel = coverModels.FirstOrDefault();
        string? subDirPath = coverModel?.FolderPath;
        if (string.IsNullOrWhiteSpace(subDirPath) ||
            string.IsNullOrWhiteSpace(coverModel?.ArtistName))
        {
            return;
        }

        DirectoryInfo subDir = new DirectoryInfo(subDirPath);
        DirectoryInfo? artistMusicFolder = GetArtistMusicFolder(subDir, coverModel.ArtistName);
        if (artistMusicFolder == null)
        {

            return;
        }

        string coverArtistFileNameWithoutExtension = Path.GetFileNameWithoutExtension(coverArtistFileName);
        bool exists = artistMusicFolder.GetFiles("*.*", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(fileName => fileName.Name.ToLower().StartsWith(coverArtistFileNameWithoutExtension.ToLower())) != null;

        if (exists)
        {
            return;
        }
        
        string spotifyArtistId = string.Empty;
        spotifyArtistIds.TryGetValue(coverModel.ArtistId, out spotifyArtistId);

        if (string.IsNullOrWhiteSpace(spotifyArtistId))
        {
            Console.WriteLine($"No artist found by '{coverModel.ArtistName}'");
            return;
        }

        string? coverUrl = _spotifyRepository.GetHighestQualityArtistCoverUrl(spotifyArtistId);

        if (string.IsNullOrEmpty(coverUrl))
        {
            Console.WriteLine($"No cover art found for '{coverModel.ArtistName}'");
            return;
        }
        
        string coverArtPath = Path.Join(artistMusicFolder.FullName, coverArtistFileName);
    
        Console.WriteLine($"Downloading cover art for {coverModel.ArtistName}");
        DownloadImage(coverUrl, coverArtPath);
    }

    private void CheckAlbumCover(MetadataPathCoverModel coverModel, string coverAlbumFileName, Dictionary<Guid, string> spotifyArtistIds)
    {
        string coverAlbumFileNameWithoutExtension = Path.GetFileNameWithoutExtension(coverAlbumFileName);
        DirectoryInfo di = new DirectoryInfo(coverModel.FolderPath);
        if (!di.Exists)
        {
            return;
        }

        bool exists = di.GetFiles()
            .FirstOrDefault(fileName => fileName.Name.ToLower().StartsWith(coverAlbumFileNameWithoutExtension.ToLower())) != null;

        if (exists)
        {
            return;
        }

        string spotifyArtistId = string.Empty;
        spotifyArtistIds.TryGetValue(coverModel.ArtistId, out spotifyArtistId);

        if (string.IsNullOrWhiteSpace(spotifyArtistId))
        {
            Console.WriteLine($"No artist found by '{coverModel.ArtistName}'");
            return;
        }

        string? coverUrl = _spotifyRepository.GetHighestQualityAlbumCoverUrl(spotifyArtistId, coverModel.AlbumName);

        if (string.IsNullOrEmpty(coverUrl))
        {
            Console.WriteLine($"No cover art found for '{coverModel.ArtistName}', '{coverModel.AlbumName}'");
            return;
        }

        string coverArtPath = Path.Join(coverModel.FolderPath, coverAlbumFileName);
            
        Console.WriteLine($"Downloading cover art for {coverModel.ArtistName}, {coverModel.AlbumName}");
        DownloadImage(coverUrl, coverArtPath);
    }
    
    private void DownloadImage(string imageUrl, string fileName)
    {
        try
        {
            using HttpClient client = new HttpClient();
            HttpResponseMessage response = client.GetAsync(imageUrl).GetAwaiter().GetResult();

            if (response.IsSuccessStatusCode)
            {
                byte[] imageBytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                File.WriteAllBytes(fileName, imageBytes);
                Console.WriteLine($"Cover art downloaded and saved as: {fileName}");
            }
            else
            {
                Console.WriteLine($"Failed to download image. HTTP Status: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while downloading the image: {ex.Message}");
        }
    }
    
    private DirectoryInfo? GetArtistMusicFolder(DirectoryInfo subDirectory, string artistName)
    {
        subDirectory = subDirectory.Parent;
        
        while (subDirectory != null && !string.Equals(subDirectory.Name, artistName, StringComparison.OrdinalIgnoreCase))
        {
            subDirectory = subDirectory.Parent;
        }
        return subDirectory;
    }
}