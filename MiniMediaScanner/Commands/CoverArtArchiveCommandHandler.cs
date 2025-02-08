using MiniMediaScanner.Models;
using MiniMediaScanner.Repositories;
using RestSharp;

namespace MiniMediaScanner.Commands;

public class CoverArtArchiveCommandHandler
{
    private readonly MetadataRepository _metadataRepository;
    private readonly ArtistRepository _artistRepository;

    public CoverArtArchiveCommandHandler(string connectionString)
    {
        _metadataRepository = new MetadataRepository(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
    }

    public void CheckAllMissingCovers(string album, string coverFileName)
    {
        _artistRepository.GetAllArtistNames()
            .AsParallel()
            .WithDegreeOfParallelism(4)
            .ForAll(artist => CheckAllMissingCovers(artist, album, coverFileName));
    }
    
    public void CheckAllMissingCovers(string artist, string album, string coverFileName)
    {
        var coverModels = _metadataRepository.GetFolderPathsByArtistForCovers(artist, album)
            .ToList();

        string coverFileNameWithoutExtension = Path.GetFileNameWithoutExtension(coverFileName);

        foreach (MetadataPathCoverModel coverModel in coverModels)
        {
            DirectoryInfo di = new DirectoryInfo(coverModel.FolderPath);
            if (!di.Exists)
            {
                continue;
            }

            bool exists = di.GetFiles()
                .FirstOrDefault(fileName => fileName.Name.ToLower().StartsWith(coverFileNameWithoutExtension.ToLower())) != null;

            if (exists)
            {
                continue;
            }

            string coverArtLink = GetCoverArtLink(coverModel.MusicBrainzReleaseId);

            if (string.IsNullOrEmpty(coverArtLink))
            {
                Console.WriteLine($"No cover art found for '{coverModel.ArtistName}', '{coverModel.AlbumName}'");
                continue;
            }

            string coverArtPath = Path.Join(coverModel.FolderPath, coverFileName);
            
            Console.WriteLine($"Downloading cover art for {coverModel.ArtistName}, {coverModel.AlbumName}");
            DownloadImage(coverArtLink, coverArtPath);
        }
    }
    
    private string? GetCoverArtLink(string releaseId)
    {
        string baseUrl = $"https://coverartarchive.org/release/{releaseId}";

        try
        {
            using RestClient client = new RestClient(baseUrl);
            RestRequest request = new RestRequest();
            var response = client.Get<CoverArtArchiveModel>(request);

            return response?.Images
                .Where(image => image.Approved)
                .Where(image => image.Front)
                .Select(image => image?.Thumbnails.Large)
                .FirstOrDefault();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }

        return string.Empty;
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
}