using System.Globalization;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;
using SmartFormat;
using SmartFormat.Utilities;

namespace MiniMediaScanner.Commands;

public class UntaggedCommandHandler
{
    private readonly ImportCommandHandler _importCommandHandler;
    private readonly MetadataRepository _metadataRepository;
    private readonly ArtistRepository _artistRepository;

    public UntaggedCommandHandler(string connectionString)
    {
        _importCommandHandler = new ImportCommandHandler(connectionString);
        _metadataRepository = new MetadataRepository(connectionString);
        _artistRepository = new ArtistRepository(connectionString);
    }
    
    public async Task GetUntaggedFilesAsync(
        string album,
        List<string> providers,
        string output, 
        List<string>? filterOut, 
        string filePath, 
        bool fileAppend)
    {
        await ParallelHelper.ForEachAsync(await _artistRepository.GetAllArtistNamesAsync(), 4, async artist =>
        {
            try
            {
                await GetUntaggedFilesAsync(artist, album, providers, output, filterOut, filePath, fileAppend);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        });
    }
    
    public async Task GetUntaggedFilesAsync(
        string artist, 
        string album,
        List<string> providers,
        string output, 
        List<string>? filterOut, 
        string filePath, 
        bool fileAppend)
    {
        var metadata = (await _metadataRepository.GetUntaggedMetadataByArtistAsync(artist, providers.ToArray()))
            .Where(metadata => string.IsNullOrWhiteSpace(album) || string.Equals(metadata.AlbumName, album, StringComparison.OrdinalIgnoreCase))
            .ToList();

        StreamWriter? fileStream = null;
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            fileStream = new StreamWriter(filePath, fileAppend);
        }
            
        metadata
            .Where(track => filterOut?.Count == 0 || !filterOut.Any(filter => $"{track.AlbumName} - {track.Title}".ToLower().Contains(filter.ToLower())))
            .Select(track => SmartFormat.Smart.Format(output, track))
            .Where(track => filterOut?.Count == 0 || !filterOut.Any(filter => track.ToLower().Contains(filter.ToLower())))
            .Distinct()
            .ToList()
            .ForEach(track =>
            {
                Console.WriteLine(track);
                fileStream?.WriteLine(track);
            });
        fileStream?.Flush();
        fileStream?.Close();
    }
}