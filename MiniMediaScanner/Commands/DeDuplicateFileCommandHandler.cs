using System.Text.RegularExpressions;
using FuzzySharp;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models;
using MiniMediaScanner.Repositories;

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

    public async Task CheckDuplicateFilesAsync(
        bool delete, 
        int accuracy, 
        List<string> extensions, 
        bool checkExtensions, 
        bool checkVersions, 
        bool checkAlbumDuplicates,
        bool checkAlbumExtensions)
    {
        await ParallelHelper.ForEachAsync(await _artistRepository.GetAllArtistNamesAsync(), 4, async artist =>
        {
            try
            {
                await CheckDuplicateFilesAsync(
                    artist, 
                    delete, 
                    accuracy, 
                    extensions, 
                    checkExtensions, 
                    checkVersions, 
                    checkAlbumDuplicates,
                    checkAlbumExtensions);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        });
    }
    
    public async Task CheckDuplicateFilesAsync(
        string artistName, 
        bool delete, 
        int accuracy, 
        List<string> extensions, 
        bool checkExtensions, 
        bool checkVersions, 
        bool checkAlbumDuplicates,
        bool checkAlbumExtensions)
    {
        Console.WriteLine($"Checking artist '{artistName}'");
        try
        {
            if (checkExtensions)
            {
                await FindDuplicateFileExtensionsAsync(artistName, delete, extensions);
            }
            if (checkAlbumExtensions)
            {
                await FindDuplicateAlbumFileExtensionsAsync(artistName, delete, extensions);
            }

            if (checkVersions)
            {
                await FindDuplicateFileVersionsAsync(artistName, delete, extensions);
            }

            if (checkAlbumDuplicates)
            {
                await FindDuplicateAlbumFileNamesAsync(artistName, delete, accuracy, extensions);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    private async Task FindDuplicateAlbumFileExtensionsAsync(string artistName, bool delete, List<string> extensions)
    {
        var duplicateFiles = (await _metadataRepository.GetDuplicateAlbumFileExtensionsAsync(artistName))
            .GroupBy(group =>
                new
                {
                    group.AlbumId,
                    group.FileName
                });
        
        foreach (var albumDuplicates in duplicateFiles)
        {
            var fileWithoutExtension = albumDuplicates.First().PathWithoutExtension;

            DuplicateAlbumFileNameModel recordToKeep = null;

            foreach (string extension in extensions)
            {
                var record = albumDuplicates
                    .FirstOrDefault(path => new FileInfo($"{path.Path.Substring(0, path.Path.LastIndexOf('.'))}.{extension}").Exists);

                if (record != null)
                {
                    recordToKeep = record;
                    break;
                }
            }
            
            if (recordToKeep == null)
            {
                continue;
            }

            var toRemove = albumDuplicates
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
    
    
    private async Task FindDuplicateAlbumFileNamesAsync(string artistName, bool delete, int accuracy, List<string> extensions)
    {
        var duplicateFiles = (await _metadataRepository.GetDuplicateAlbumFileNamesAsync(artistName, accuracy))
            .GroupBy(group =>
                new
                {
                    group.AlbumId
                });
        
        foreach (var albumDuplicates in duplicateFiles)
        {
            var fileGroups = new List<List<DuplicateAlbumFileNameModel>>();
            foreach (var file in albumDuplicates)
            {
                var matchingGroup = fileGroups
                    .FirstOrDefault(group => group.Any(n => Fuzz.Ratio(file.FileName, n.FileName) >= accuracy));

                if (matchingGroup != null)
                {
                    matchingGroup.Add(file); // Add to the found group
                }
                else
                {
                    fileGroups.Add(new List<DuplicateAlbumFileNameModel> { file }); // Create a new group
                }
            }

            foreach (var duplicateFileVersions in fileGroups)
            {
                DuplicateAlbumFileNameModel recordToKeep = null;

                foreach (string extension in extensions)
                {
                    var record = duplicateFileVersions
                        .FirstOrDefault(path => new FileInfo($"{path.Path.Substring(0, path.Path.LastIndexOf('.'))}.{extension}").Exists);

                    if (record != null)
                    {
                        recordToKeep = record;
                        break;
                    }
                }
                
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
                Console.WriteLine();
            }
        }
    }
    
    private async Task FindDuplicateFileExtensionsAsync(string artistName, bool delete, List<string> extensions)
    {
        var duplicateFiles = (await _metadataRepository.GetDuplicateFileExtensionsAsync(artistName))
            .GroupBy(group => group.FilePathWithoutExtension);

        foreach (var duplicateFileVersions in duplicateFiles)
        {
            var fileWithoutExtension = duplicateFileVersions.First().FilePathWithoutExtension;
            var recordToKeep = (await Task.WhenAll(
                    extensions
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
    
    private async Task FindDuplicateFileVersionsAsync(string artistName, bool delete, List<string> extensions)
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
                        extensions
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
                await _metadataRepository.DeleteMetadataRecordsAsync(new List<string>(new string[] { possibleDuplicateFile.MetadataId.ToString() }));
            }
            else
            {
                Console.WriteLine($"Duplicate file {possibleDuplicateFile.Path}");
            }
            Console.WriteLine($"");
        }
    }
}