using System.Diagnostics;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;
using Newtonsoft.Json;
using TagLib.Flac;
using File = System.IO.File;

namespace MiniMediaScanner.Commands;

public class FixVersioningCommandHandler
{
    private readonly MetadataRepository _metadataRepository;
    private readonly MediaTagWriteService _mediaTagWriteService;
    private readonly ImportCommandHandler _importCommandHandler;
    private readonly ArtistRepository _artistRepository;

    public FixVersioningCommandHandler(string connectionString)
    {
        _metadataRepository = new MetadataRepository(connectionString);
        _importCommandHandler = new ImportCommandHandler(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
        _mediaTagWriteService = new MediaTagWriteService();
    }

    public async Task FixDiscVersioningAsync(string album, int discIncrement, List<string> trackFilters, bool autoConfirm)
    {
        if (autoConfirm)
        {
            await ParallelHelper.ForEachAsync(await _artistRepository.GetAllArtistNamesAsync(), 4, async artist =>
            {
                try
                {
                    await FixDiscVersioningAsync(artist, album, discIncrement, trackFilters, autoConfirm);
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
                await FixDiscVersioningAsync(artist, album, discIncrement, trackFilters, autoConfirm);
            }
        }
    }
    
    public async Task FixDiscVersioningAsync(string artist, string album, int discIncrement, List<string> trackFilters, bool autoConfirm)
    {
        var metadata = (await _metadataRepository.GetMetadataByArtistAsync(artist))
            .Where(metadata => string.IsNullOrWhiteSpace(album) || string.Equals(metadata.AlbumName, album, StringComparison.OrdinalIgnoreCase))
            .Where(metadata => File.Exists(metadata.Path))
            .ToList();

        var groupedByAlbumId = metadata.GroupBy(metadata => metadata.AlbumId);
        
        List<MetadataModel> updateMetadata = new List<MetadataModel>();

        foreach (var group in groupedByAlbumId)
        {
            var groupedTrackDisc = group
                .Where(m => m.Tag_Disc < discIncrement)
                .GroupBy(m => new
                {
                    m.Tag_Track, 
                    m.Tag_Disc
                })
                .Where(group => group.Count() > 1);

            foreach (var groupTrackDisc in groupedTrackDisc)
            {
                var remixTracks = groupTrackDisc
                    .OrderByDescending(m => m.Title.Length)
                    .Take(groupTrackDisc.Count() - 1);

                foreach (var track in remixTracks.Where(track => 
                             trackFilters.Count == 0 ||
                             trackFilters.Any(filter => track.Title.ToLower().Contains(filter.ToLower()))))
                {
                    if (!updateMetadata.Any(m => m.Path == track.Path))
                    {
                        updateMetadata.Add(track);
                    }
                }
            }
        }

        if (updateMetadata.Count == 0)
        {
            Console.WriteLine("Everything seems correct already");
            return;
        }
        
        updateMetadata
            .ForEach(track => Console.WriteLine($"Album: {track.AlbumName}, Track: {track.Tag_Track}, Disc: {track.Tag_Disc} => {track.Tag_Disc + discIncrement}, Title: {track.Title}, File: {track.Path}"));
        
        Console.WriteLine("Confirm changes? (Y/y or N/n)");
        bool confirm = autoConfirm || Console.ReadLine()?.ToLower() == "y";

        if (confirm)
        {
            foreach (var track in updateMetadata)
            {
                int newDisc = track.Tag_Disc + discIncrement;
                if (await _mediaTagWriteService.SaveTagAsync(new FileInfo(track.Path), "disc", $"{newDisc}"))
                {
                    await _importCommandHandler.ProcessFileAsync(track.Path);
                }
            }
        }
    }
}