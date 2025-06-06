using System.Diagnostics;
using ATL;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models.MusicBrainz;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MiniMediaScanner.Commands;

public class SplitTagCommandHandler
{
    private readonly MetadataRepository _metadataRepository;
    private readonly ArtistRepository _artistRepository;
    private readonly MediaTagWriteService _mediaTagWriteService;
    private readonly ImportCommandHandler _importCommandHandler;
    private readonly FileMetaDataService _fileMetaDataService;

    public SplitTagCommandHandler(string connectionString)
    {
        _metadataRepository = new MetadataRepository(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
        _mediaTagWriteService = new MediaTagWriteService();
        _importCommandHandler = new ImportCommandHandler(connectionString);
        _fileMetaDataService = new FileMetaDataService();
    }
    
    public async Task SplitTagsAsync(string album, string tag, bool confirm, string writetag, 
        bool overwriteTag, string seperator, bool updateReadTag, bool updateReadTagOriginalValue,
        bool updateWriteTagOriginalValue)
    {
        if (confirm)
        {
            await ParallelHelper.ForEachAsync(await _artistRepository.GetAllArtistNamesAsync(), 4, async artist =>
            {
                try
                {
                    await SplitTagsAsync(artist, album, tag, confirm, writetag, overwriteTag, seperator, updateReadTag, updateReadTagOriginalValue, updateWriteTagOriginalValue);
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
                await SplitTagsAsync(artist, album, tag, confirm, writetag, overwriteTag, seperator, updateReadTag, updateReadTagOriginalValue, updateWriteTagOriginalValue);
            }
        }
    }

    public async Task SplitTagsAsync(string artist, string album, string tag, bool confirm, string writetag, 
        bool overwriteTag, string seperator, bool updateReadTag, bool updateReadTagOriginalValue,
        bool updateWriteTagOriginalValue)
    {
        var metadata = (await _metadataRepository.GetMetadataByTagValueRecordsAsync(artist, tag, seperator))
            .Where(metadata => string.IsNullOrWhiteSpace(album) || 
                               string.Equals(metadata.Album, album, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Console.WriteLine($"Checking artist '{artist}', found {metadata.Count} tracks to process");

        foreach (var record in metadata.Where(r => new FileInfo(r.Path).Exists))
        {
            try
            {
                await ProcessFileAsync(record, tag, confirm, writetag, overwriteTag, seperator, updateReadTag, updateReadTagOriginalValue, updateWriteTagOriginalValue);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
                
    private async Task ProcessFileAsync(MetadataInfo metadata, string tag, bool autoConfirm, string writetag, 
        bool overwriteTagValue, string seperator, bool updateReadTag, bool updateReadTagOriginalValue,
        bool updateWriteTagOriginalValue)
    {
        bool trackInfoUpdated = false;

        var mediaTags = JsonConvert.DeserializeObject<Dictionary<string, string>>(metadata.Tag_AllJsonTags);

        KeyValuePair<string, string>? mediaTargetTag = mediaTags.FirstOrDefault(pair => string.Equals(pair.Key, tag, StringComparison.OrdinalIgnoreCase));

        if (mediaTargetTag == null)
        {
            Console.WriteLine($"Tag '{tag}' not found (corrupt Json in database?) for '{metadata.Path}'");
            return;
        }
        
        string[] splitValue = mediaTargetTag.Value.Value.Split(seperator, StringSplitOptions.RemoveEmptyEntries);

        if (splitValue.Length <= 1)
        {
            return;
        }

        Track track = new Track(metadata.Path);
        var metadataInfo = _fileMetaDataService.GetMetadataInfo(new FileInfo(track.Path));
        
        string newWriteTagValue = updateWriteTagOriginalValue ? mediaTargetTag.Value.Value : splitValue.First();
        _mediaTagWriteService.UpdateTag(track, metadataInfo, writetag, newWriteTagValue, ref trackInfoUpdated, overwriteTagValue);

        if (updateReadTag && !string.Equals(tag, writetag))
        {
            string newReadTagValue = updateReadTagOriginalValue ? mediaTargetTag.Value.Value : splitValue.First();
            _mediaTagWriteService.UpdateTag(track, metadataInfo, tag, newReadTagValue, ref trackInfoUpdated, overwriteTagValue);
        }
        
        if (!trackInfoUpdated)
        {
            return;
        }
        
        Console.WriteLine("Confirm changes? (Y/y or N/n)");
        bool confirm = autoConfirm || Console.ReadLine()?.ToLower() == "y";

        try
        {
            if (confirm && await _mediaTagWriteService.SafeSaveAsync(track))
            {
                await _importCommandHandler.ProcessFileAsync(metadata.Path);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}