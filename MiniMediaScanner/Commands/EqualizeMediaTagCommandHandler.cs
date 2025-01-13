using System.Diagnostics;
using MiniMediaScanner.Models;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;
using Newtonsoft.Json;
using TagLib.Flac;
using File = System.IO.File;

namespace MiniMediaScanner.Commands;

public class EqualizeMediaTagCommandHandler
{
    private readonly MetadataRepository _metadataRepository;
    private readonly MediaTagWriteService _mediaTagWriteService;
    private readonly ImportCommandHandler _importCommandHandler;
    private readonly ArtistRepository _artistRepository;

    public EqualizeMediaTagCommandHandler(string connectionString)
    {
        _metadataRepository = new MetadataRepository(connectionString);
        _importCommandHandler = new ImportCommandHandler(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
        _mediaTagWriteService = new MediaTagWriteService();
    }

    public void EqualizeTags(string album, string tag, bool autoConfirm)
    {
        _artistRepository.GetAllArtistNames()
            .ForEach(artist => EqualizeTags(artist, album, tag, autoConfirm));
    }
    
    public void EqualizeTags(string artist, string album, string tag, bool autoConfirm)
    {
        var metadata = _metadataRepository.GetMetadataByArtist(artist)
            .Where(metadata => string.IsNullOrWhiteSpace(album) || string.Equals(metadata.AlbumName, album, StringComparison.OrdinalIgnoreCase))
            .Where(metadata => File.Exists(metadata.Path))
            .ToList();

        bool success = false;

        var groupedByAlbumId = metadata.GroupBy(metadata => metadata.AlbumId);

        foreach (var group in groupedByAlbumId)
        {
            switch (tag.ToLower())
            {
                case "date":
                    success = ProcessDate(group.ToList(), artist, album, autoConfirm);
                    break;
            }
        }
    }

    private bool ProcessDate(List<MetadataModel> metadataFiles, string artist, string album, bool autoConfirm)
    {
        const string Tag = "date";
        if (metadataFiles.Any(m => string.IsNullOrWhiteSpace(m.AllJsonTags)))
        {
            Console.WriteLine($"Unable to process '{album}' of '{artist}', missing serialized json tags in database.");
            return false;
        }

        var metadataTags = metadataFiles
            .Select(m => new
            {
                Metadata = m,
                Tags = JsonConvert.DeserializeObject<Dictionary<string, string>>(m.AllJsonTags)
            })
            .ToList();

        var valueDates = metadataTags
            .Where(m => m.Tags.ContainsKey(Tag))
            .GroupBy(m => m.Tags[Tag])
            .Select(m => new
            {
                Count = m.Count(),
                Date = m.Key
            })
            .OrderByDescending(m => m.Count)
            .ThenByDescending(m => m.Date.Length);
        
        var valueDate = valueDates.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(valueDate?.Date))
        {
            Console.WriteLine($"Unable to process '{album}' of '{artist}', no date found in tags.");
            return false;
        }

        Console.WriteLine("Values found to write:");
        foreach (var groupedDate in valueDates)
        {
            Console.WriteLine($"Count: {groupedDate.Count}, Date: {groupedDate.Date}");
        }
        
        var fileDifferences = metadataTags
            .Where(m => !m.Tags.ContainsKey(Tag) || m.Tags[Tag] != valueDate.Date);
        
        if(fileDifferences.Count() == 0)
        {
            Console.WriteLine($"Files are already having the correct date tags, skipping '{album}' of '{artist}'.");
            return false;
        }
        
        foreach (var metadata in fileDifferences)
        {
            Console.WriteLine($"File {metadata.Metadata.Path}, Date '{metadata.Tags[Tag]}' => '{valueDate.Date}'");
        }
        
        Console.WriteLine("Confirm changes? (Y/y or N/n)");
        bool confirm = autoConfirm || Console.ReadLine()?.ToLower() == "y";

        if (!confirm)
        {
            return false;
        }
        
        foreach (var metadata in fileDifferences)
        {
            if (_mediaTagWriteService.SaveTag(new FileInfo(metadata.Metadata.Path), Tag, valueDate.Date))
            {
                _importCommandHandler.ProcessFile(metadata.Metadata.Path);
                Console.WriteLine($"Written {Tag} '{valueDate.Date}' to '{metadata.Metadata.Path}'");
            }
            else
            {
                Console.WriteLine($"Failed to save tag to file '{metadata.Metadata.Path}'");
            }
        }
        
        return true;
    }
}