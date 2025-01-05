using System.Globalization;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using MiniMediaScanner.Models;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class NormalizeFileCommandHandler
{
    private readonly StringNormalizerService _stringNormalizerService;
    private readonly MediaTagWriteService _tagWriteService;
    private readonly ImportCommandHandler _importCommandHandler;
    private readonly MetadataRepository _metadataRepository;
    
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
    }
    
    public void NormalizeFiles(
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
        const int limit = 1000;
        int offset = 0;

        while (true)
        {
            var metadata = _metadataRepository.GetAllMetadata(offset, limit);
            offset += limit;

            if (metadata.Count == 0)
            {
                break;
            }
            
            //due to I/O limitations max 4 threads is probably the best for now
            //making more threads won't make anything faster but rather make it slooow
            metadata
                .AsParallel()
                .WithDegreeOfParallelism(4)
                .ForAll(file =>
                {
                    try
                    {
                        bool success = ProcessFile(file, normalizeArtistName, normalizeAlbumName, normalizeTitleName, overwrite,
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
            
            Console.WriteLine($"Updated files: {_updateFiles}, artist names {_updateArtistNames}, album names {_updateAlbumNames}, title names {_updateTitleNames}");
        }
        Console.WriteLine($"Can update files: {_updateFiles}, artist names {_updateArtistNames}, album names {_updateAlbumNames}, title names {_updateTitleNames}");
    }

    private bool ProcessFile(MetadataModel file,
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
        
        bool updatedArtistName = !string.Equals(artistNormalized, file.ArtistName);
        bool updatedalbumName = !string.Equals(albumNormalized, file.AlbumName);
        bool updatedTitleName = !string.Equals(titleNormalized, file.Title);

        string oldPath = file.Path;
        FileInfo fileInfo = new FileInfo(file.Path);

        if (!fileInfo.Exists)
        {
            return false;
        }
        
        DirectoryInfo? subDirectory = fileInfo.Directory!;
        for (int i = 0; i < subDirectoryDepth; i++)
        {
            subDirectory = subDirectory?.Parent;
            if (subDirectory == null)
            {
                Console.WriteLine($"Depth is too low, directory does not exist for '{fileInfo.Directory?.FullName}'");
                return false;
            }
        }
        
        string newFileName = $"{GetFormatName(fileFormat, artistNormalized, albumNormalized, file.Tag_Track, titleNormalized, directorySeperator)}{fileInfo.Extension}";
        string newDirectoryName = $"{GetFormatName(directoryFormat, artistNormalized, albumNormalized, file.Tag_Track, titleNormalized, directorySeperator)}";
        string partialNewPath = Path.Combine(newDirectoryName, newFileName);
        string newFullPath = Path.Combine(subDirectory.FullName, partialNewPath).Trim();
        
        FileInfo newFileInfo = new FileInfo(newFullPath);

        if (!updatedArtistName && 
            !updatedalbumName && 
            !updatedTitleName && 
            newFileInfo.Name == fileInfo.Name)
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
        bool success = _tagWriteService.Save(fileInfo, artistNormalized, albumNormalized, titleNormalized);

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
            _importCommandHandler.ProcessFile(newFullPath); //updata database
        }
        else
        {
            _importCommandHandler.ProcessFile(fileInfo.FullName); //update database
        }

        lock (_consoleLock)
        {
            Console.WriteLine($"File: {oldPath}");
            Console.WriteLine($"Moving to: {newFullPath}");
            Console.WriteLine($"Success: {success}");
            Console.WriteLine($"Updated artist: {updatedArtistName}, artist name: {file.ArtistName} {(updatedArtistName ? $" => {artistNormalized}" : string.Empty)}");
            Console.WriteLine($"Updated album: {updatedalbumName}, album name: {file.AlbumName} {(updatedalbumName ? $" => {albumNormalized}" : string.Empty)}");
            Console.WriteLine($"Updated title: {updatedTitleName}, title: {file.Title} {(updatedTitleName ? $" => {titleNormalized}" : string.Empty)}");
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
    
    public string GetFormatName(string format, 
        string artist, 
        string album, 
        int track, 
        string title, 
        string seperator)
    {
        format = format.Replace("{artist}", ReplaceDirectorySeparators(artist, seperator));
        format = format.Replace("{album}", ReplaceDirectorySeparators(album, seperator));
        format = format.Replace("{track}", track.ToString("D2"));
        format = format.Replace("{title}", ReplaceDirectorySeparators(title, seperator));
        format = format.Trim();
        return format;
    }

    private string ReplaceDirectorySeparators(string input, string seperator)
    {
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
}