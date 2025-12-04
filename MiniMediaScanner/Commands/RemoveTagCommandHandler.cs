using System.Diagnostics;
using ATL;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;
using Newtonsoft.Json.Linq;

namespace MiniMediaScanner.Commands;

public class RemoveTagCommandHandler
{
    private readonly MetadataRepository _metadataRepository;
    private readonly ArtistRepository _artistRepository;
    private readonly MediaTagWriteService _mediaTagWriteService;
    private readonly ImportCommandHandler _importCommandHandler;
    private readonly FileMetaDataService _fileMetaDataService;

    public RemoveTagCommandHandler(string connectionString)
    {
        _metadataRepository = new MetadataRepository(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
        _mediaTagWriteService = new MediaTagWriteService();
        _importCommandHandler = new ImportCommandHandler(connectionString);
        _fileMetaDataService = new FileMetaDataService();
    }

    public async Task RemoveTagFromMediaAsync(string album, List<string> tagNames, bool autoConfirm)
    {
        if (autoConfirm)
        {
            await ParallelHelper.ForEachAsync(await _artistRepository.GetAllArtistNamesAsync(), 4, async artist =>
            {
                try
                {
                    await RemoveTagFromMediaAsync(artist, album, tagNames, autoConfirm);
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
                await RemoveTagFromMediaAsync(artist, album, tagNames, autoConfirm);
            }
        }
    }

    public async Task RemoveTagFromMediaAsync(string artist, string album, List<string> tagNames, bool autoConfirm)
    {
        try
        {
            var metadatas = (await _metadataRepository.GetMetadataByTagRecordsAsync(artist, tagNames))
                .Where(metadata => string.IsNullOrWhiteSpace(album) || 
                                   string.Equals(metadata.Album, album, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Console.WriteLine($"Checking artist '{artist}', found {metadatas.Count} tracks to process");

            foreach (var metadata in metadatas)
            {
                try
                {
                    await ProcessFileAsync(metadata, tagNames, autoConfirm);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
                
    private async Task ProcessFileAsync(MetadataInfo metadata, List<string> tagNames, bool autoConfirm)
    {
        if (!new FileInfo(metadata.Path).Exists)
        {
            return;
        }
        
        bool trackInfoUpdated = false;
        Track track = new Track(metadata.Path);
        var metadataInfo = _fileMetaDataService.GetMetadataInfo(track);

        foreach (string tagName in tagNames)
        {
            UpdateTag(track, tagName, string.Empty, ref trackInfoUpdated, metadataInfo);
        }

        if (!trackInfoUpdated)
        {
            return;
        }

        Console.WriteLine("Confirm changes? (Y/y or N/n)");
        bool confirm = autoConfirm || Console.ReadLine()?.ToLower() == "y";
        
        if (confirm && await _mediaTagWriteService.SafeSaveAsync(track))
        {
            await _importCommandHandler.ProcessFileAsync(metadata.Path);
        }
    }

    private void UpdateTag(Track track, string tagName, string? value, ref bool trackInfoUpdated, MetadataInfo metadataInfo)
    {
        tagName = _mediaTagWriteService.GetFieldName(track, tagName);
        
        string orgValue = string.Empty;
        bool tempIsUpdated = false;
        _mediaTagWriteService.UpdateTrackTag(track, tagName, value, ref tempIsUpdated, ref orgValue, metadataInfo);

        if (tempIsUpdated)
        {
            Console.WriteLine($"Updating tag '{tagName}' => '{value}'");
            trackInfoUpdated = true;
        }
    }
}