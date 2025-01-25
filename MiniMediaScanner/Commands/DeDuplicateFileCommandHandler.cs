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
    
    public void CheckDuplicateFiles(string artistName, bool delete)
    {
        var artistNames = _artistRepository.GetAllArtistNames();

        if (!string.IsNullOrWhiteSpace(artistName))
        {
            artistNames = artistNames
                .Where(a => a.ToLower() == artistName.ToLower())
                .ToList();
        }

        //regex for files ending with (1).flac, (2).mp3 etc
        string regexFilter = @" \([0-9]*\)(?=\.([a-zA-Z0-9]{2,5})$)";
        
        foreach (string artist in artistNames)
        {
            List<MetadataModel> possibleDuplicateFiles = _metadataRepository.PossibleDuplicateFiles(artist);

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

                if (delete)
                {
                    Console.WriteLine($"Delete duplicate file {possibleDuplicateFile.Path}");
                    duplicatefileInfo.Delete();
                }
                else
                {
                    Console.WriteLine($"Duplicate file {possibleDuplicateFile.Path}");
                }
            }
        }
    }
}