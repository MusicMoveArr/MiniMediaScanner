using System.Diagnostics;
using ATL;
using MiniMediaScanner.Models.MusicBrainz;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MiniMediaScanner.Commands;

public class SplitTagCommandHandler
{
    private readonly AcoustIdService _acoustIdService;
    private readonly MusicBrainzAPIService _musicBrainzAPIService;
    private readonly MetadataRepository _metadataRepository;
    private readonly ArtistRepository _artistRepository;
    private readonly MediaTagWriteService _mediaTagWriteService;
    private readonly ImportCommandHandler _importCommandHandler;
    private readonly StringNormalizerService _normalizerService;
    private readonly MusicBrainzArtistRepository _musicBrainzArtistRepository;

    public SplitTagCommandHandler(string connectionString)
    {
        _acoustIdService = new AcoustIdService();
        _musicBrainzAPIService = new MusicBrainzAPIService();
        _metadataRepository = new MetadataRepository(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
        _mediaTagWriteService = new MediaTagWriteService();
        _importCommandHandler = new ImportCommandHandler(connectionString);
        _normalizerService = new StringNormalizerService();
        _musicBrainzArtistRepository = new MusicBrainzArtistRepository(connectionString);
    }
    
    public void SplitTags(string album, string tag, bool confirm, string writetag, 
        bool overwriteTag, string seperator, bool updateReadTag, bool updateReadTagOriginalValue,
        bool updateWriteTagOriginalValue)
    {
        _artistRepository.GetAllArtistNames()
            .ForEach(artist => SplitTags(artist, album, tag, confirm, writetag, overwriteTag, seperator, updateReadTag, updateReadTagOriginalValue, updateWriteTagOriginalValue));
    }

    public void SplitTags(string artist, string album, string tag, bool confirm, string writetag, 
        bool overwriteTag, string seperator, bool updateReadTag, bool updateReadTagOriginalValue,
        bool updateWriteTagOriginalValue)
    {
        var metadata = _metadataRepository.GetMetadataByTagValueRecords(artist, tag, seperator)
            .Where(metadata => string.IsNullOrWhiteSpace(album) || 
                               string.Equals(metadata.Album, album, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Console.WriteLine($"Checking artist '{artist}', found {metadata.Count} tracks to process");

        metadata
            .Where(metadata => new FileInfo(metadata.Path).Exists)
            .ToList()
            .ForEach(metadata =>
            {
                try
                {
                    ProcessFile(metadata, tag, confirm, writetag, overwriteTag, seperator, updateReadTag, updateReadTagOriginalValue, updateWriteTagOriginalValue);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            });
    }
                
    private void ProcessFile(MetadataInfo metadata, string tag, bool autoConfirm, string writetag, 
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
        
        string newWriteTagValue = updateWriteTagOriginalValue ? mediaTargetTag.Value.Value : splitValue.First();
        UpdateTag(track, writetag, newWriteTagValue, ref trackInfoUpdated, overwriteTagValue);

        if (updateReadTag && !string.Equals(tag, writetag))
        {
            string newReadTagValue = updateReadTagOriginalValue ? mediaTargetTag.Value.Value : splitValue.First();
            UpdateTag(track, tag, newReadTagValue, ref trackInfoUpdated, overwriteTagValue);
        }
        
        if (!trackInfoUpdated)
        {
            return;
        }
        
        Console.WriteLine("Confirm changes? (Y/y or N/n)");
        bool confirm = autoConfirm || Console.ReadLine()?.ToLower() == "y";

        try
        {
            if (confirm && _mediaTagWriteService.SafeSave(track))
            {
                _importCommandHandler.ProcessFile(metadata.Path);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    private void UpdateTag(Track track, string tagName, string? value, ref bool trackInfoUpdated, bool overwriteTagValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (int.TryParse(value, out int intValue) && intValue == 0)
        {
            return;
        }
        
        tagName = _mediaTagWriteService.GetFieldName(track, tagName);
        value = _normalizerService.ReplaceInvalidCharacters(value);
        
        if (!overwriteTagValue &&
            (track.AdditionalFields.ContainsKey(tagName) ||
             !string.IsNullOrWhiteSpace(track.AdditionalFields[tagName])))
        {
            return;
        }

        string orgValue = string.Empty;
        bool tempIsUpdated = false;
        _mediaTagWriteService.UpdateTrackTag(track, tagName, value, ref tempIsUpdated, ref orgValue);

        if (tempIsUpdated)
        {
            Console.WriteLine($"Updating tag '{tagName}' value '{orgValue}' => '{value}'");
            trackInfoUpdated = true;
        }
    }
}