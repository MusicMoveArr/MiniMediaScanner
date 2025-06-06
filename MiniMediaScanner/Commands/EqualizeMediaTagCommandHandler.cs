using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;
using Newtonsoft.Json;
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

    public async Task EqualizeTagsAsync(string album, string tag, string writetag, bool autoConfirm)
    {
        if (autoConfirm)
        {
            await ParallelHelper.ForEachAsync(await _artistRepository.GetAllArtistNamesAsync(), 4, async artist =>
            {
                try
                {
                    await EqualizeTagsAsync(artist, album, tag, writetag, autoConfirm);
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
                await EqualizeTagsAsync(artist, album, tag, writetag, autoConfirm);
            }
        }
    }
    
    public async Task EqualizeTagsAsync(string artist, string album, string tag, string writetag, bool autoConfirm)
    {
        var metadata = (await _metadataRepository.GetMetadataByArtistAsync(artist))
            .Where(metadata => string.IsNullOrWhiteSpace(album) || string.Equals(metadata.AlbumName, album, StringComparison.OrdinalIgnoreCase))
            .Where(metadata => File.Exists(metadata.Path))
            .ToList();

        bool success = false;

        var groupedByAlbumId = metadata.GroupBy(metadata => metadata.AlbumId);

        foreach (var group in groupedByAlbumId)
        {
            string albumName = group.First().AlbumName;
            success = await ProcessGenericTagAsync(group.ToList(), artist, albumName, autoConfirm, tag, writetag);
        }
    }

    private async Task<bool> ProcessGenericTagAsync(List<MetadataModel> metadataFiles, string artist, string album, bool autoConfirm, string tagName, string writeTagName)
    {
        if (metadataFiles.Any(m => string.IsNullOrWhiteSpace(m.Tag_AllJsonTags)))
        {
            Console.WriteLine($"Unable to process '{album}' of '{artist}', missing serialized json tags in database.");
            return false;
        }

        var metadataTags = metadataFiles
            .Select(m => new
            {
                Metadata = m,
                Tags = JsonConvert.DeserializeObject<Dictionary<string, string>>(m.Tag_AllJsonTags)
                    !.ToDictionary(StringComparer.OrdinalIgnoreCase)
            })
            .ToList();

        var tagValues = metadataTags
            .Where(m => m.Tags.ContainsKey(tagName))
            .Where(m => (int.TryParse(m.Tags[tagName], out int tagValue) && tagValue > 0) || !int.TryParse(m.Tags[tagName], out int _))
            .GroupBy(m => m.Tags[tagName])
            .Select(m => new
            {
                Count = m.Count(),
                Date = m.Key
            })
            .OrderByDescending(m => m.Count)
            .ThenByDescending(m => m.Date.Length);
        
        var tagValue = tagValues.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(tagValue?.Date))
        {
            Console.WriteLine($"Unable to process '{album}' of '{artist}', no {tagName} found in tags.");
            return false;
        }

        Console.WriteLine("Values found to write:");
        foreach (var groupedDate in tagValues)
        {
            Console.WriteLine($"Count: {groupedDate.Count}, {tagName}: {groupedDate.Date}");
        }
        
        var fileDifferences = metadataTags
            .Where(m => !m.Tags.ContainsKey(tagName) || m.Tags[tagName] != tagValue.Date);
        
        if(fileDifferences.Count() == 0)
        {
            Console.WriteLine($"Files are already having the correct date tags, skipping '{album}' of '{artist}'.");
            return false;
        }
        
        foreach (var metadata in fileDifferences)
        {
            string dateValue = metadata.Tags.ContainsKey(tagName) ? metadata.Tags[tagName] : "";
            Console.WriteLine($"File {metadata.Metadata.Path}, {tagName} '{dateValue}' => '{tagValue.Date}'");
        }
        
        Console.WriteLine("Confirm changes? (Y/y or N/n)");
        bool confirm = autoConfirm || Console.ReadLine()?.ToLower() == "y";

        if (!confirm)
        {
            return false;
        }
        
        foreach (var metadata in fileDifferences)
        {
            if (await _mediaTagWriteService.SaveTagAsync(new FileInfo(metadata.Metadata.Path), writeTagName, tagValue.Date))
            {
                await _importCommandHandler.ProcessFileAsync(metadata.Metadata.Path);
                Console.WriteLine($"Written {writeTagName} '{tagValue.Date}' to '{metadata.Metadata.Path}'");
            }
            else
            {
                Console.WriteLine($"Failed to save tag to file '{metadata.Metadata.Path}'");
            }
        }
        
        return true;
    }
}