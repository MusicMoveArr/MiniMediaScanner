using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using MiniMediaScanner.Helpers;
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
        Console.WriteLine($"Checking artist '{artistName}'");
        try
        {
            await FindDuplicateAlbumFileNamesAsync(artistName, delete);
            await FindDuplicateFileExtensionsAsync(artistName, delete);
            await FindDuplicateFileVersionsAsync(artistName, delete);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    private async Task FindDuplicateAlbumFileNamesAsync(string artistName, bool delete)
    {
        var duplicateFiles = (await _metadataRepository.GetDuplicateAlbumFileNamesAsync(artistName))
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
                    await _metadataRepository.DeleteMetadataRecordsAsync(new List<string>(new string[] { file.MetadataId.ToString() }));
                }
                else
                {
                    Console.WriteLine($"Duplicate file {file.Path}");
                }
            }
            Console.WriteLine($"");
        }
    }
    
    private async Task FindDuplicateFileExtensionsAsync(string artistName, bool delete)
    {
        var duplicateFiles = (await _metadataRepository.GetDuplicateFileExtensionsAsync(artistName))
            .GroupBy(group => group.FilePathWithoutExtension);

        foreach (var duplicateFileVersions in duplicateFiles)
        {
            var fileWithoutExtension = duplicateFileVersions.First().FilePathWithoutExtension;
            var recordToKeep = (await Task.WhenAll(
                    ImportCommandHandler.MediaFileExtensions
                        .Select(async ext =>
                            (await _metadataRepository.GetMetadataByPathAsync(fileWithoutExtension + "." + ext))
                            .FirstOrDefault())
                ))
                .Where(metadata => metadata != null)
                .FirstOrDefault(metadata => new FileInfo(metadata.Path).Exists);
            
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
                    await _metadataRepository.DeleteMetadataRecordsAsync(new List<string>(new string[] { file.MetadataId.ToString() }));
                }
                else
                {
                    Console.WriteLine($"Duplicate file {file.Path}");
                }
            }
            Console.WriteLine($"");
        }
    }
    
    private async Task FindDuplicateFileVersionsAsync(string artistName, bool delete)
    {
        //regex for files ending with (1).flac, (2).mp3 etc
        string regexFilter = @" \([0-9]*\)(?=\.([a-zA-Z0-9]{2,5})$)";
        List<MetadataModel> possibleDuplicateFiles = await _metadataRepository.GetDuplicateFileVersionsAsync(artistName);

        foreach (MetadataModel possibleDuplicateFile in possibleDuplicateFiles)
        {
            string nonDuplicateFile = Regex.Replace(possibleDuplicateFile.Path, regexFilter, string.Empty);
            MetadataModel? nonDuplicateRecord = (await _metadataRepository
                .GetMetadataByPathAsync(nonDuplicateFile))
                .FirstOrDefault();

            //try to find another non-duplicate version, different media extension
            if (nonDuplicateRecord == null)
            {
                string extension = Path.GetExtension(nonDuplicateFile).ToLower().Replace(".", "");
                string fileWithoutExtension = Path.ChangeExtension(nonDuplicateFile, "");
                
                nonDuplicateRecord = (await Task.WhenAll(
                        ImportCommandHandler.MediaFileExtensions
                            .Where(ext => ext != extension)
                            .Select(ext => fileWithoutExtension + ext)
                            .Select(async path =>
                                (await _metadataRepository.GetMetadataByPathAsync(path))
                                .FirstOrDefault())
                    ))
                    .FirstOrDefault(metadata => metadata != null);

                if (!string.IsNullOrWhiteSpace(nonDuplicateRecord?.Path))
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