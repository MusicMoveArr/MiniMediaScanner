using System.Text;
using System.Text.RegularExpressions;
using ATL;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;
using SmartFormat;
using File = System.IO.File;

namespace MiniMediaScanner.Commands;

public class FixEncodingCommandHandler
{
    private readonly MetadataRepository _metadataRepository;
    private readonly MediaTagWriteService _mediaTagWriteService;
    private readonly ImportCommandHandler _importCommandHandler;
    private readonly ArtistRepository _artistRepository;
    private readonly FileMetaDataService _fileMetaDataService;
    private object _consoleLock = new();
    
    public string Album { get; set; }
    public Encoding FromEncoding { get; set; }
    public Encoding TargetEncoding { get; set; }
    public string WhitelistRegex { get; set; }
    public string VerifyRegex { get; set; }
    public string BlacklistRegex { get; set; }
    public bool AutoConfirm { get; set; }
    public bool Overwrite { get; set; }
    public string FileFormat { get; set; }
    public string DirectoryFormat { get; set; }
    public string DirectorySeperator { get; set; }
    public bool DryRun { get; set; }
    public List<string> Tags { get; set; }
    public int SubDirectoryDepth { get; set; }
    public bool CheckFilename { get; set; }

    public FixEncodingCommandHandler(string connectionString)
    {
        _metadataRepository = new MetadataRepository(connectionString);
        _importCommandHandler = new ImportCommandHandler(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
        _mediaTagWriteService = new MediaTagWriteService();
        _fileMetaDataService = new FileMetaDataService();
    }

    public async Task FixEncodingAsync()
    {
        if (AutoConfirm)
        {
            await ParallelHelper.ForEachAsync(await _artistRepository.GetAllArtistNamesAsync(), 1, async artist =>
            {
                try
                {
                    await FixEncodingAsync(artist);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            });
        }
        else
        {
            foreach (var artist in await _artistRepository.GetAllArtistNamesAsync())
            {
                await FixEncodingAsync(artist);
            }
        }
    }
    
    public async Task FixEncodingAsync(string artist)
    {
        var metadatas = (await _metadataRepository.GetMetadataByArtistAsync(artist))
            .Where(metadata => string.IsNullOrWhiteSpace(Album) || string.Equals(metadata.AlbumName, Album, StringComparison.OrdinalIgnoreCase))
            .Where(metadata => File.Exists(metadata.Path))
            .ToList();
        
        foreach (var metadata in metadatas)
        {
            try
            {
                await ProcessFileAsync(metadata);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\r\n" + e.StackTrace);
            }
        }
    }
    
    private async Task ProcessFileAsync(MetadataModel metadata)
    {
        FileInfo fileInfo = new FileInfo(metadata.Path);
        Track track = new Track(metadata.Path);
        DirectoryInfo? musicRootDirectory = GetMusicRootFolder(fileInfo, track.Artist, SubDirectoryDepth);
        if(musicRootDirectory == null)
        {
            Console.WriteLine($"Unable to determine the music root folder '{fileInfo.Directory?.FullName}'");
            return;
        }
        
        var metadataInfo = await _fileMetaDataService.GetMetadataInfoAsync(new FileInfo(track.Path));
        bool trackInfoUpdated = false;

        foreach (string tag in this.Tags)
        {
            string value = _mediaTagWriteService.GetTagValue(track, tag);

            if (string.IsNullOrWhiteSpace(value) ||
                !Regex.IsMatch(value, WhitelistRegex))
            {
                continue;
            }
            
            string fixedValue = TargetEncoding.GetString(FromEncoding.GetBytes(value));
            bool isMatch = Regex.IsMatch(fixedValue, VerifyRegex);
            bool blacklistIsMatch = Regex.IsMatch(fixedValue, BlacklistRegex);
            if (isMatch && !blacklistIsMatch)
            {
                _mediaTagWriteService.UpdateTag(track, metadataInfo, tag, fixedValue, ref trackInfoUpdated, true);
            }
        }
        

        if (!trackInfoUpdated && !CheckFilename)
        {
            return;
        }
        
        metadataInfo = _fileMetaDataService.GetMetadataInfo(track);
        
        string newFileName = $"{GetFormatName(metadataInfo, FileFormat, DirectorySeperator)}{fileInfo.Extension}";
        string newDirectoryName = $"{GetFormatName(metadataInfo, DirectoryFormat, DirectorySeperator)}";
        newDirectoryName = GetDirectoryCaseInsensitive(musicRootDirectory, newDirectoryName);

        string partialNewPath = Path.Combine(newDirectoryName, newFileName);
        string newFullPath = Path.Combine(musicRootDirectory.FullName, partialNewPath).Trim();
        
        bool fileMatched = Regex.IsMatch(partialNewPath, VerifyRegex) &&
                           !Regex.IsMatch(partialNewPath, BlacklistRegex);

        if (!fileMatched || string.Equals(metadata.Path, newFullPath))
        {
            return;
        }
    
        FileInfo newFileInfo = new FileInfo(newFullPath);
            
        if (newFileInfo.Name.Length > 250)
        {
            lock (_consoleLock)
            {
                Console.WriteLine($"New Filename: {newFileInfo.Name}");
                Console.WriteLine($"Skipped reason: Filename too long, length is {newFileInfo.Name.Length}");
                Console.WriteLine();
            }
            return;
        }
        if (newFileInfo.Exists && !Overwrite)
        {
            lock (_consoleLock)
            {
                Console.WriteLine($"File: {fileInfo.FullName}");
                Console.WriteLine($"Skipped reason: Already exists");
                Console.WriteLine();
            }
            return;
        }

        Console.WriteLine($"For file: {metadata.Path}");
        Console.WriteLine($"Saving to: {newFullPath}");
        Console.Write("Confirm changes? (Y/y or N/n)");
        bool confirm = this.AutoConfirm || Console.ReadLine()?.ToLower() == "y";
        if (!DryRun && confirm)
        {
            if (!newFileInfo.Directory.Exists)
            {
                newFileInfo.Directory.Create();
            }
            
            if (trackInfoUpdated && await _mediaTagWriteService.SafeSaveAsync(track))
            {
                await _importCommandHandler.ProcessFileAsync(metadata.Path);
            }
            
            fileInfo.MoveTo(newFullPath, true);
            await _importCommandHandler.ProcessFileAsync(newFullPath);
            if (metadata.Path != newFullPath)
            {
                await _metadataRepository.DeleteMetadataRecordsAsync([metadata.MetadataId.ToString()]);
            }
        }
    }
    
    public string GetFormatName(MetadataInfo file,
        string format, 
        string seperator)
    {
        file.Artist = ReplaceDirectorySeparators(file.ArtistName, seperator);
        file.Title = ReplaceDirectorySeparators(file.Title, seperator);
        file.Album = ReplaceDirectorySeparators(file.AlbumName, seperator);
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