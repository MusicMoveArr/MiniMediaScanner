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

public class NormalizeFileCommandHandler
{
    private readonly StringNormalizerService _stringNormalizerService;
    private readonly MediaTagWriteService _tagWriteService;
    private readonly ImportCommandHandler _importCommandHandler;
    private readonly MetadataRepository _metadataRepository;
    private readonly ArtistRepository _artistRepository;
    
    private int _updateFiles = 0;
    private int _updateAlbumNames = 0;
    private int _updateArtistNames = 0;
    private int _updateTitleNames = 0;
    private object _consoleLock = new();

    public NormalizeFileCommandHandler(string connectionString)
    {
        _stringNormalizerService = new StringNormalizerService();
        _tagWriteService = new MediaTagWriteService();
        _importCommandHandler = new ImportCommandHandler(connectionString);
        _metadataRepository = new MetadataRepository(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
    }
    
    

    public async Task NormalizeFilesAsync(string album,
        bool normalizeArtistName,
        bool normalizeAlbumName,
        bool normalizeTitleName,
        bool overwrite,
        int subDirectoryDepth = 0,
        bool rename = false, 
        string fileFormat = "",
        string directoryFormat = "",
        string directorySeperator = "_")
    {
        await ParallelHelper.ForEachAsync(await _artistRepository.GetAllArtistNamesAsync(), 4, async artist =>
        {
            try
            {
                await NormalizeFilesAsync(artist, album,
                    normalizeArtistName, 
                    normalizeAlbumName, 
                    normalizeTitleName, 
                    overwrite,
                    subDirectoryDepth, 
                    rename, 
                    fileFormat, 
                    directoryFormat, 
                    directorySeperator);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        });
    }
    
    public async Task NormalizeFilesAsync(
        string artist,
        string album,
        bool normalizeArtistName,
        bool normalizeAlbumName,
        bool normalizeTitleName,
        bool overwrite,
        int subDirectoryDepth = 0,
        bool rename = false, 
        string fileFormat = "",
        string directoryFormat = "",
        string directorySeperator = "_")
    {
        var metadatas = (await _metadataRepository.GetMetadataByArtistAsync(artist))
            .Where(metadata => string.IsNullOrWhiteSpace(album) || string.Equals(metadata.AlbumName, album, StringComparison.OrdinalIgnoreCase))
            .ToList();

        //due to I/O limitations max 4 threads is probably the best for now
        //making more threads won't make anything faster but rather make it slooow
        await ParallelHelper.ForEachAsync(metadatas, 4, async metadata =>
        {
            try
            {
                bool success = await ProcessFileAsync(metadata, normalizeArtistName, normalizeAlbumName, normalizeTitleName, overwrite,
                    subDirectoryDepth, rename, fileFormat, directoryFormat, directorySeperator);

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
        
        Console.WriteLine($"Can update files: {_updateFiles}, artist names {_updateArtistNames}, album names {_updateAlbumNames}, title names {_updateTitleNames}");
    }

    private async Task<bool> ProcessFileAsync(MetadataModel file,
        bool normalizeArtistName,
        bool normalizeAlbumName,
        bool normalizeTitleName,
        bool overwrite,
        int subDirectoryDepth = 0,
        bool rename = false, 
        string fileFormat = "",
        string directoryFormat = "",
        string directorySeperator = "_")
    {
        string artistNormalized = normalizeArtistName ? _stringNormalizerService.NormalizeText(file.ArtistName) : file.ArtistName;
        string albumNormalized = normalizeAlbumName ? _stringNormalizerService.NormalizeText(file.AlbumName) : file.AlbumName;
        string titleNormalized = normalizeTitleName ? _stringNormalizerService.NormalizeText(file.Title) : file.Title;

        if (string.IsNullOrWhiteSpace(artistNormalized) ||
            string.IsNullOrWhiteSpace(albumNormalized) ||
            string.IsNullOrWhiteSpace(titleNormalized))
        {
            Console.WriteLine($"Skipped file due either missing tags Artist, Album, Title for '{file.Path}'");
            return false;
        }
        
        bool updatedArtistName = !string.Equals(artistNormalized, file.ArtistName);
        bool updatedalbumName = !string.Equals(albumNormalized, file.AlbumName);
        bool updatedTitleName = !string.Equals(titleNormalized, file.Title);
        
        string oldPath = file.Path;
        FileInfo fileInfo = new FileInfo(file.Path);

        if (!fileInfo.Exists)
        {
            return false;
        }
        
        DirectoryInfo? musicRootDirectory = GetMusicRootFolder(fileInfo, file.ArtistName, subDirectoryDepth);
        if(musicRootDirectory == null)
        {
            Console.WriteLine($"Depth is too low, directory does not exist for '{fileInfo.Directory?.FullName}'");
            return false;
        }

        string oldArtistName = file.ArtistName;
        string oldAlbumName = file.AlbumName;
        string oldTitle = file.Title;

        file.ArtistName = artistNormalized;
        file.AlbumName = albumNormalized;
        file.Title = titleNormalized;
        
        string newFileName = $"{GetFormatName(file, fileFormat, directorySeperator)}{fileInfo.Extension}";
        string newDirectoryName = $"{GetFormatName(file, directoryFormat, directorySeperator)}";
        newDirectoryName = GetDirectoryCaseInsensitive(musicRootDirectory, newDirectoryName);

        string partialNewPath = Path.Combine(newDirectoryName, newFileName);
        string newFullPath = Path.Combine(musicRootDirectory.FullName, partialNewPath).Trim();
        
        FileInfo newFileInfo = new FileInfo(newFullPath);

        if (!updatedArtistName && 
            !updatedalbumName && 
            !updatedTitleName && 
            newFileInfo.FullName == fileInfo.FullName)
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
        
        //write new tag values to old filename first
        bool success = await _tagWriteService.SaveAsync(fileInfo, artistNormalized, albumNormalized, titleNormalized);

        if (!success)
        {
            lock (_consoleLock)
            {
                Console.WriteLine($"Failed to save file: {fileInfo.FullName}");
                Console.WriteLine();
            }
            return false;
        }
        
        //rename the file to it's new location
        if (rename)
        {
            if (!newFileInfo.Directory.Exists)
            {
                newFileInfo.Directory.Create();
            }
            fileInfo.MoveTo(newFullPath, true);
            await _importCommandHandler.ProcessFileAsync(newFullPath);
        }
        else
        {
            await _importCommandHandler.ProcessFileAsync(fileInfo.FullName);
        }

        lock (_consoleLock)
        {
            Console.WriteLine($"File: {oldPath}");
            Console.WriteLine($"Moving to: {newFullPath}");
            Console.WriteLine($"Success: {success}");
            Console.WriteLine($"Updated artist: {updatedArtistName}, artist name: {oldArtistName} {(updatedArtistName ? $" => {artistNormalized}" : string.Empty)}");
            Console.WriteLine($"Updated album: {updatedalbumName}, album name: {oldAlbumName} {(updatedalbumName ? $" => {albumNormalized}" : string.Empty)}");
            Console.WriteLine($"Updated title: {updatedTitleName}, title: {oldTitle} {(updatedTitleName ? $" => {titleNormalized}" : string.Empty)}");
            Console.WriteLine();
        }
                
        if (updatedArtistName)
        {
            _updateArtistNames++;
        }
        if (updatedalbumName)
        {
            _updateAlbumNames++;
        }
        if (updatedTitleName)
        {
            _updateTitleNames++;
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
        DirectoryInfo subDirectory = fileInfo.Directory!;

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

        while (subDirectory != null && !string.Equals(subDirectory.Name, artistName, StringComparison.OrdinalIgnoreCase))
        {
            subDirectory = subDirectory.Parent;
        }
        return subDirectory.Parent;
    }
}