using System.Diagnostics;
using ATL;
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

    public RemoveTagCommandHandler(string connectionString)
    {
        _metadataRepository = new MetadataRepository(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
        _mediaTagWriteService = new MediaTagWriteService();
        _importCommandHandler = new ImportCommandHandler(connectionString);
    }

    public void RemoveTagFromMedia(string album, string tagName, bool autoConfirm)
    {
        _artistRepository.GetAllArtistNames()
            .ForEach(artist => RemoveTagFromMedia(artist, album, tagName, autoConfirm));
    }

    public void RemoveTagFromMedia(string artist, string album, string tagName, bool autoConfirm)
    {
        var metadatas = _metadataRepository.GetMetadataByTagRecords(artist, tagName)
            .Where(metadata => string.IsNullOrWhiteSpace(album) || 
                               string.Equals(metadata.Album, album, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Console.WriteLine($"Checking artist '{artist}', found {metadatas.Count} tracks to process");

        metadatas
            .ForEach(metadata =>
            {
                try
                {
                    ProcessFile(metadata, tagName, autoConfirm);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            });
    }
                
    private void ProcessFile(MetadataInfo metadata, string tagName, bool autoConfirm)
    {
        bool trackInfoUpdated = false;
        Track track = new Track(metadata.Path);
        UpdateTag(track, tagName, string.Empty, ref trackInfoUpdated);

        if (!trackInfoUpdated)
        {
            return;
        }
        
        Console.WriteLine("Confirm changes? (Y/y or N/n)");
        bool confirm = autoConfirm || Console.ReadLine()?.ToLower() == "y";
        
        if (trackInfoUpdated && confirm && _mediaTagWriteService.SafeSave(track))
        {
            _importCommandHandler.ProcessFile(metadata.Path);
        }
    }

    private void UpdateTag(Track track, string tagName, string? value, ref bool trackInfoUpdated)
    {
        tagName = _mediaTagWriteService.GetFieldName(track, tagName);
        
        bool tempIsUpdated = false;
        _mediaTagWriteService.UpdateTrackTag(track, tagName, value, ref tempIsUpdated);

        if (tempIsUpdated)
        {
            Console.WriteLine($"Updating tag '{tagName}' => '{value}'");
            trackInfoUpdated = true;
        }
    }
}