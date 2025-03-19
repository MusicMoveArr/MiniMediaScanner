using MiniMediaScanner.Helpers;
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

    public async Task CheckAllMissingCoversAsync(string album, string coverAlbumFileName, string coverArtistFileName)
    {
        await ParallelHelper.ForEachAsync(await _artistRepository.GetAllArtistNamesAsync(), 4, async artist =>
        {
            try
            {
                await CheckAllMissingCoversAsync(artist, album, coverAlbumFileName, coverArtistFileName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        });
    }
    
    public async Task CheckAllMissingCoversAsync(string artist, string album, string coverAlbumFileName, string coverArtistFileName)
    {
        Console.WriteLine($"Checking artist '{artist}'");
        var coverModels = (await _metadataRepository.GetFolderPathsByArtistForCoversAsync(artist, album))
            .ToList();
        
        Dictionary<Guid, string> spotifyArtistIds = (await Task.WhenAll(
                coverModels
                    .DistinctBy(cover => cover.ArtistId)
                    .Select(async cover => new
                    {
                        SpotifyArtistId = await _matchRepository.GetBestSpotifyMatchAsync(cover.ArtistId, cover.ArtistName),
                        CoverArtistId = cover.ArtistId
                    })
            ))
            .Where(cover => !string.IsNullOrWhiteSpace(cover.SpotifyArtistId))
            .ToDictionary(cover => cover.CoverArtistId, cover => cover.SpotifyArtistId!);
        
        if (spotifyArtistIds.Count == 0)
        {
            Console.WriteLine($"No spotify artist found in the database for '{artist}'");
            return;
        }

        foreach (MetadataPathCoverModel coverModel in coverModels)
        {
            await CheckAlbumCoverAsync(coverModel, coverAlbumFileName, spotifyArtistIds);
        }

        await CheckArtistCoverAsync(coverModels, coverArtistFileName, spotifyArtistIds);
    }

    private async Task CheckArtistCoverAsync(List<MetadataPathCoverModel> coverModels, 
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

        string? coverUrl = await _spotifyRepository.GetHighestQualityArtistCoverUrlAsync(spotifyArtistId);

        if (string.IsNullOrEmpty(coverUrl))
        {
            Console.WriteLine($"No cover art found for '{coverModel.ArtistName}'");
            return;
        }
        
        string coverArtPath = Path.Join(artistMusicFolder.FullName, coverArtistFileName);
    
        Console.WriteLine($"Downloading cover art for {coverModel.ArtistName}");
        await DownloadImageAsync(coverUrl, coverArtPath);
    }

    private async Task CheckAlbumCoverAsync(MetadataPathCoverModel coverModel, string coverAlbumFileName, Dictionary<Guid, string> spotifyArtistIds)
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

        string? coverUrl = await _spotifyRepository.GetHighestQualityAlbumCoverUrlAsync(spotifyArtistId, coverModel.AlbumName);

        if (string.IsNullOrEmpty(coverUrl))
        {
            Console.WriteLine($"No cover art found for '{coverModel.ArtistName}', '{coverModel.AlbumName}'");
            return;
        }

        string coverArtPath = Path.Join(coverModel.FolderPath, coverAlbumFileName);
            
        Console.WriteLine($"Downloading cover art for {coverModel.ArtistName}, {coverModel.AlbumName}");
        await DownloadImageAsync(coverUrl, coverArtPath);
    }
    
    private async Task DownloadImageAsync(string imageUrl, string fileName)
    {
        try
        {
            using HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(imageUrl);

            if (response.IsSuccessStatusCode)
            {
                byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
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