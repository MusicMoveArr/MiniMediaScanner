using System.Diagnostics;
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

    public void FixDiscVersioning(string album, int discIncrement, List<string> trackFilters, bool autoConfirm)
    {
        _artistRepository.GetAllArtistNames()
            .ForEach(artist => FixDiscVersioning(artist, album, discIncrement, trackFilters, autoConfirm));
    }
    
    public void FixDiscVersioning(string artist, string album, int discIncrement, List<string> trackFilters, bool autoConfirm)
    {
        var metadata = _metadataRepository.GetMetadataByArtist(artist)
            .Where(metadata => string.IsNullOrWhiteSpace(album) || string.Equals(metadata.AlbumName, album, StringComparison.OrdinalIgnoreCase))
            .Where(metadata => File.Exists(metadata.Path))
            .ToList();

        bool success = false;

        var groupedByAlbumId = metadata.GroupBy(metadata => metadata.AlbumId);
        
        List<MetadataModel> updateMetadata = new List<MetadataModel>();

        foreach (var group in groupedByAlbumId)
        {
            var groupedTrackDisc = group
                .Where(m => m.Disc < discIncrement)
                .GroupBy(m => new
                {
                    m.Track, 
                    m.Disc
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
            .ForEach(track => Console.WriteLine($"Album: {track.AlbumName}, Track: {track.Track}, Disc: {track.Disc} => {track.Disc + discIncrement}, Title: {track.Title}, File: {track.Path}"));
        
        Console.WriteLine("Confirm changes? (Y/y or N/n)");
        bool confirm = autoConfirm || Console.ReadLine()?.ToLower() == "y";

        if (confirm)
        {
            foreach (var track in updateMetadata)
            {
                int newDisc = track.Disc + discIncrement;
                if (_mediaTagWriteService.SaveTag(new FileInfo(track.Path), "disc", $"{newDisc}"))
                {
                    _importCommandHandler.ProcessFile(track.Path);
                }
            }
        }
    }
}