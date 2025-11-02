using MiniMediaScanner.Helpers;
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

    public async Task CheckAllMissingCoversAsync(string album, string coverFileName)
    {
        await ParallelHelper.ForEachAsync(await _artistRepository.GetAllArtistNamesAsync(), 4, async artist =>
        {
            try
            {
                await CheckAllMissingCoversAsync(artist, album, coverFileName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        });
    }
    
    public async Task CheckAllMissingCoversAsync(string artist, string album, string coverFileName)
    {
        var coverModels = 
            (await _metadataRepository.GetFolderPathsByArtistForCoversAsync(artist, album))
            .Where(cover => !string.IsNullOrWhiteSpace(cover.MusicBrainzReleaseId))
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

            string coverArtLink = await GetCoverArtLinkAsync(coverModel.MusicBrainzReleaseId);

            if (string.IsNullOrEmpty(coverArtLink))
            {
                Console.WriteLine($"No cover art found for '{coverModel.ArtistName}', '{coverModel.AlbumName}'");
                continue;
            }

            string coverArtPath = Path.Join(coverModel.FolderPath, coverFileName);
            
            Console.WriteLine($"Downloading cover art for {coverModel.ArtistName}, {coverModel.AlbumName}");
            await DownloadImageAsync(coverArtLink, coverArtPath);
        }
    }
    
    private async Task<string?> GetCoverArtLinkAsync(string releaseId)
    {
        string baseUrl = $"https://coverartarchive.org/release/{releaseId}";

        try
        {
            using RestClient client = new RestClient(baseUrl);
            RestRequest request = new RestRequest();
            var response = await client.GetAsync<CoverArtArchiveModel>(request);

            return response?.Images
                .Where(image => image.Approved)
                .Where(image => image.Front)
                .Select(image => image?.Thumbnails.Large)
                .FirstOrDefault();
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"An error occurred: {ex.Message}");
        }

        return string.Empty;
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
}