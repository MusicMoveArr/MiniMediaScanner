using ATL;
using MiniMediaScanner.Models;
using MiniMediaScanner.Repositories;
using RestSharp;

namespace MiniMediaScanner.Commands;

public class CoverExtractCommandHandler
{
    private readonly MetadataRepository _metadataRepository;
    private readonly ArtistRepository _artistRepository;

    public CoverExtractCommandHandler(string connectionString)
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

            var metadataPaths = _metadataRepository
                .GetPathByLikePath(coverModel.FolderPath)
                .Where(file => new FileInfo(file).Exists)
                .ToList();
            
            bool success = false;
            
            foreach (var path in metadataPaths)
            {
                try
                {
                    if (ExtractCoverArt(path, coverFileName))
                    {
                        success = true;
                        Console.WriteLine($"Extracted cover art for {coverModel.ArtistName}, {coverModel.AlbumName}");
                        break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            if (!success)
            {
                Console.WriteLine($"No cover art found for '{coverModel.ArtistName}', '{coverModel.AlbumName}'");
            }
        }
    }

    private bool ExtractCoverArt(string filePath, string coverFileName)
    {
        FileInfo fileInfo = new FileInfo(filePath);
        Track track = new Track(fileInfo.FullName);
        if (track.EmbeddedPictures?.Any() == true)
        {
            var pictureInfo = track.EmbeddedPictures.FirstOrDefault();
            string coverFilePath = Path.Join(fileInfo.Directory.FullName, coverFileName);
            File.WriteAllBytes(coverFilePath, pictureInfo.PictureData);
            return true;
        }

        return false;
    }
}