using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using MiniMediaScanner.Models;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class DeDuplicateFileCommandHandler
{
    private readonly ArtistRepository _artistRepository;
    private readonly MetadataRepository _metadataRepository;

    public DeDuplicateFileCommandHandler(string connectionString)
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
        Console.WriteLine($"Checking artist '{artistName}'");
        try
        {
            FindDuplicateAlbumFileNames(artistName, delete);
            FindDuplicateFileExtensions(artistName, delete);
            FindDuplicateFileVersions(artistName, delete);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    private void FindDuplicateAlbumFileNames(string artistName, bool delete)
    {
        var duplicateFiles = _metadataRepository.GetDuplicateAlbumFileNames(artistName)
            .GroupBy(group => new { group.AlbumId, group.FileName });

        foreach (var duplicateFileVersions in duplicateFiles)
        {
            var recordToKeep = duplicateFileVersions
                .FirstOrDefault(path => new FileInfo(path.Path).Exists);

            if (recordToKeep == null)
            {
                continue;
            }

            var toRemove = duplicateFileVersions
                .Where(file => !string.Equals(recordToKeep.Path, file.Path))
                .Where(file => new FileInfo(file.Path).Exists)
                .ToList();

            if (toRemove.Count == 0)
            {
                continue;
            }

            Console.WriteLine($"Keeping file {recordToKeep.Path}");
            foreach (var file in toRemove)
            {
                if (delete)
                {
                    Console.WriteLine($"Delete duplicate file {file.Path}");
                    new FileInfo(file.Path).Delete();
                    _metadataRepository.DeleteMetadataRecords(new List<string>(new string[] { file.MetadataId.ToString() }));
                }
                else
                {
                    Console.WriteLine($"Duplicate file {file.Path}");
                }
            }
            Console.WriteLine($"");
        }
    }
    
    private void FindDuplicateFileExtensions(string artistName, bool delete)
    {
        var duplicateFiles = _metadataRepository.GetDuplicateFileExtensions(artistName)
            .GroupBy(group => group.FilePathWithoutExtension);

        foreach (var duplicateFileVersions in duplicateFiles)
        {
            var fileWithoutExtension = duplicateFileVersions.First().FilePathWithoutExtension;
            var recordToKeep = ImportCommandHandler.MediaFileExtensions
                .Select(ext => fileWithoutExtension + "." + ext)
                .Select(path => _metadataRepository
                    .GetMetadataByPath(path)
                    .FirstOrDefault())
                .Where(path => path != null)
                .Where(path => new FileInfo(path.Path).Exists)
                .FirstOrDefault(nonRecord => nonRecord != null);

            if (recordToKeep == null)
            {
                continue;
            }

            var toRemove = duplicateFileVersions
                .Where(file => !string.Equals(recordToKeep.Path, file.Path))
                .Where(file => new FileInfo(file.Path).Exists)
                .ToList();

            if (toRemove.Count == 0)
            {
                continue;
            }

            Console.WriteLine($"Keeping file {recordToKeep.Path}");
            foreach (var file in toRemove)
            {
                if (delete)
                {
                    Console.WriteLine($"Delete duplicate file {file.Path}");
                    new FileInfo(file.Path).Delete();
                    _metadataRepository.DeleteMetadataRecords(new List<string>(new string[] { file.MetadataId.ToString() }));
                }
                else
                {
                    Console.WriteLine($"Duplicate file {file.Path}");
                }
            }
            Console.WriteLine($"");
        }
    }
    
    private void FindDuplicateFileVersions(string artistName, bool delete)
    {
        //regex for files ending with (1).flac, (2).mp3 etc
        string regexFilter = @" \([0-9]*\)(?=\.([a-zA-Z0-9]{2,5})$)";
        List<MetadataModel> possibleDuplicateFiles = _metadataRepository.GetDuplicateFileVersions(artistName);

        foreach (MetadataModel possibleDuplicateFile in possibleDuplicateFiles)
        {
            string nonDuplicateFile = Regex.Replace(possibleDuplicateFile.Path, regexFilter, string.Empty);
            MetadataModel? nonDuplicateRecord = _metadataRepository
                .GetMetadataByPath(nonDuplicateFile)
                .FirstOrDefault();

            //try to find another non-duplicate version, different media extension
            if (nonDuplicateRecord == null)
            {
                string extension = Path.GetExtension(nonDuplicateFile).ToLower().Replace(".", "");
                string fileWithoutExtension = Path.ChangeExtension(nonDuplicateFile, "");
                
                nonDuplicateRecord = ImportCommandHandler.MediaFileExtensions
                    .Where(ext => ext != extension)
                    .Select(ext => fileWithoutExtension + ext)
                    .Select(path => _metadataRepository
                        .GetMetadataByPath(path)
                        .FirstOrDefault())
                    .FirstOrDefault(nonRecord => nonRecord != null);

                if (nonDuplicateRecord != null)
                {
                    nonDuplicateFile = nonDuplicateRecord.Path;
                }
            }
            
            FileInfo nonDuplicatefileInfo = new FileInfo(nonDuplicateFile);
            FileInfo duplicatefileInfo = new FileInfo(possibleDuplicateFile.Path);
            if (!nonDuplicatefileInfo.Exists  ||
                !duplicatefileInfo.Exists ||
                nonDuplicateRecord == null)
            {
                continue;
            }

            if (!string.Equals(nonDuplicateRecord.Title, possibleDuplicateFile.Title, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(nonDuplicateRecord.AlbumId.ToString(), possibleDuplicateFile.AlbumId.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Console.WriteLine($"Keeping file {nonDuplicateRecord.Path}");
            if (delete)
            {
                Console.WriteLine($"Delete duplicate file {possibleDuplicateFile.Path}");
                duplicatefileInfo.Delete();
            }
            else
            {
                Console.WriteLine($"Duplicate file {possibleDuplicateFile.Path}");
            }
            Console.WriteLine($"");
        }
    }
}