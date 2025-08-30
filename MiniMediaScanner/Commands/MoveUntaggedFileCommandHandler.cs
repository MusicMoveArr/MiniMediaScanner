using System.Globalization;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;
using SmartFormat;
using SmartFormat.Utilities;

namespace MiniMediaScanner.Commands;

public class MoveUntaggedCommandHandler
{
    private readonly ImportCommandHandler _importCommandHandler;
    private readonly MetadataRepository _metadataRepository;
    private readonly ArtistRepository _artistRepository;
    
    private int _updateFiles = 0;
    private object _consoleLock = new();

    public MoveUntaggedCommandHandler(string connectionString)
    {
        _importCommandHandler = new ImportCommandHandler(connectionString);
        _metadataRepository = new MetadataRepository(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
    }
    
    public async Task MoveUntaggedFilesAsync(string album,
        bool overwrite,
        string targetFolder,
        string fileFormat = "",
        string directoryFormat = "",
        string directorySeperator = "_",
        bool dryRun = false)
    {
        await ParallelHelper.ForEachAsync(await _artistRepository.GetAllArtistNamesAsync(), 4, async artist =>
        {
            try
            {
                await MoveUntaggedFilesAsync(artist, album,
                    overwrite,
                    targetFolder,
                    fileFormat, 
                    directoryFormat, 
                    directorySeperator,
                    dryRun);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        });
    }
    
    public async Task MoveUntaggedFilesAsync(
        string artist,
        string album,
        bool overwrite,
        string targetFolder,
        string fileFormat = "",
        string directoryFormat = "",
        string directorySeperator = "_",
        bool dryRun = false)
    {
        var metadatas = (await _metadataRepository.GetUntaggedMetadataByArtistAsync(artist, ["musicbrainz", "tidal", "spotify", "deezer"]))
            .Where(metadata => string.IsNullOrWhiteSpace(album) || string.Equals(metadata.AlbumName, album, StringComparison.OrdinalIgnoreCase))
            .ToList();

        //due to I/O limitations max 4 threads is probably the best for now
        //making more threads won't make anything faster but rather make it slooow
        await ParallelHelper.ForEachAsync(metadatas, 1, async metadata =>
        {
            try
            {
                bool success = await ProcessFileAsync(metadata, overwrite, targetFolder,
                    fileFormat, directoryFormat, directorySeperator, dryRun);

                if (success)
                {
                    _updateFiles++;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        });
        
        Console.WriteLine($"Moved files: {_updateFiles}");
    }

    private async Task<bool> ProcessFileAsync(MetadataModel file,
        bool overwrite,
        string targetFolder,
        string fileFormat = "",
        string directoryFormat = "",
        string directorySeperator = "_",
        bool dryRun = false)
    {
        string oldPath = file.Path;
        FileInfo fileInfo = new FileInfo(file.Path);

        if (!fileInfo.Exists)
        {
            return false;
        }
        
        string newFileName = $"{GetFormatName(file, fileFormat, directorySeperator)}{fileInfo.Extension}";
        string newDirectoryName = $"{GetFormatName(file, directoryFormat, directorySeperator)}";
        newDirectoryName =  Path.Combine(targetFolder, newDirectoryName);

        string partialNewPath = Path.Combine(newDirectoryName, newFileName);
        string newFullPath = Path.Combine(targetFolder, partialNewPath).Trim();
        
        FileInfo newFileInfo = new FileInfo(newFullPath);

        if (string.Equals(newFileInfo.FullName, fileInfo.FullName))
        {
            return false;
        }
        
        if (newFileInfo.Name.Length > 250)
        {
            Console.WriteLine($"New Filename: {newFileInfo.Name}");
            Console.WriteLine($"Skipped reason: Filename too long, length is {newFileInfo.Name.Length}");
            Console.WriteLine();
            return false;
        }
        
        if (newFileInfo.Exists && !overwrite)
        {
            lock (_consoleLock)
            {
                Console.WriteLine($"File: {fileInfo.FullName}");
                Console.WriteLine($"Skipped reason: Already exists");
                Console.WriteLine();
            }

            return false;
        }
        
        //rename the file to it's new location
        if (!dryRun)
        {
            if (!newFileInfo.Directory.Exists)
            {
                newFileInfo.Directory.Create();
            }
        
            fileInfo.MoveTo(newFullPath, true);
            //await _importCommandHandler.ProcessFileAsync(newFullPath);

            lock (_consoleLock)
            {
                Console.WriteLine($"File: {oldPath}");
                Console.WriteLine($"Moved to: {newFullPath}");
                Console.WriteLine();
            }
        }
        else
        {
            lock (_consoleLock)
            {
                Console.WriteLine($"File: {oldPath}");
                Console.WriteLine($"Will move to: {newFullPath}");
                Console.WriteLine();
            }
        }

        return true;
    }
    
    public string GetFormatName(MetadataModel file,
        string format, 
        string seperator)
    {
        file.ArtistName = ReplaceDirectorySeparators(file.ArtistName, seperator);
        file.Title = ReplaceDirectorySeparators(file.Title, seperator);
        file.AlbumName = ReplaceDirectorySeparators(file.AlbumName, seperator);
        format = Smart.Format(format, file);
        format = format.Trim();
        return format;
    }

    private string ReplaceDirectorySeparators(string? input, string seperator)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }
        if (input.Contains('/'))
        {
            input = input.Replace("/", seperator);
        }
        else if (input.Contains('\\'))
        {
            input = input.Replace("\\", seperator);
        }

        return input;
    }

    private string GetDirectoryCaseInsensitive(DirectoryInfo directory, string directoryPath)
    {
        DirectoryInfo tempDirectory = directory;
        string[] subDirectories = directoryPath.Split(Path.DirectorySeparatorChar);
        List<string> newSubDirectoryNames = new List<string>();
        foreach (string subDirectory in subDirectories)
        {
            if (tempDirectory == null)
            {
                newSubDirectoryNames.Add(subDirectory);
                continue;
            }
            string dirName = GetNextDirectoryCaseInsensitive(tempDirectory, subDirectory, out tempDirectory);
            newSubDirectoryNames.Add(dirName);
        }
        
        return string.Join(Path.DirectorySeparatorChar, newSubDirectoryNames);
    }

    private string GetNextDirectoryCaseInsensitive(DirectoryInfo directory, string subDirectory, out DirectoryInfo? targetDir)
    {
        targetDir = directory.GetDirectories()
            .OrderBy(dir => dir.Name)
            .FirstOrDefault(dir => string.Equals(dir.Name, subDirectory, StringComparison.OrdinalIgnoreCase));

        if (targetDir != null)
        {
            return targetDir.Name;
        }
        return subDirectory;
    }

    private DirectoryInfo? GetMusicRootFolder(FileInfo fileInfo, string artistName, int subDirectoryDepth)
    {
        DirectoryInfo? subDirectory = fileInfo.Directory!;

        if (subDirectoryDepth > 0)
        {
            for (int i = 0; i < subDirectoryDepth; i++)
            {
                subDirectory = subDirectory?.Parent;
                if (subDirectory == null)
                {
                    return null;
                }
            }
            return subDirectory;
        }
        
        //for albums that have the exact same name as the artist name
        while (subDirectory != null && string.Equals(subDirectory.Name, artistName, StringComparison.OrdinalIgnoreCase))
        {
            if (!string.Equals(subDirectory?.Parent?.Name, artistName, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
            subDirectory = subDirectory?.Parent;
        }

        while (subDirectory != null && !string.Equals(subDirectory.Name, artistName, StringComparison.OrdinalIgnoreCase))
        {
            subDirectory = subDirectory?.Parent;
        }
        return subDirectory?.Parent;
    }
}