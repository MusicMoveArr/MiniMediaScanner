using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models;
using MiniMediaScanner.Repositories;
using ATL;
using FuzzySharp;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class FixCollectionsCommandHandler
{
    private readonly MetadataRepository _metadataRepository;
    private readonly MediaTagWriteService _mediaTagWriteService;
    private readonly ImportCommandHandler _importCommandHandler;
    private readonly FileMetaDataService _fileMetaDataService;

    private const int BulkProcess = 100;
    private const int FuzzyMatchRatio = 80;

    public FixCollectionsCommandHandler(string connectionString)
    {
        _metadataRepository = new MetadataRepository(connectionString);
        _mediaTagWriteService = new MediaTagWriteService();
        _importCommandHandler = new ImportCommandHandler(connectionString);
        _fileMetaDataService = new FileMetaDataService();
    }

    public async Task FindMissingArtistsAsync(string artist, string targetLabel, string targetCopyright, string albumRegex, string addArtist, bool confirm)
    {
        if (!string.IsNullOrWhiteSpace(targetLabel))
        {
            List<MetadataModel> metadata = await _metadataRepository.GetMetadataByTagMissingArtistAsync(artist, "label", targetLabel, addArtist, albumRegex);
            Console.WriteLine($"Found '{metadata.Count}' files with missing artist '{addArtist}', searched with Label tag");
            await ProcessMetadataAsync(metadata, addArtist, confirm);
        }
        if (!string.IsNullOrWhiteSpace(targetCopyright))
        {
            List<MetadataModel> metadata = await _metadataRepository.GetMetadataByTagMissingArtistAsync(artist, "copyright", targetCopyright, addArtist, albumRegex);
            Console.WriteLine($"Found '{metadata.Count}' files with missing artist '{addArtist}', searched with Copyright tag");
            await ProcessMetadataAsync(metadata, addArtist, confirm);
        }
    }

    private async Task ProcessMetadataAsync(List<MetadataModel> metadatas, string addArtist, bool confirm)
    {
        int offset = 0;
            while (true)
            {
                List<MetadataModel> bulkset = metadatas
                    .Skip(offset)
                    .Take(BulkProcess)
                    .ToList();

                if (bulkset.Count == 0)
                {
                    break;
                }

                List<Track> updatedTracks = new List<Track>();

                foreach (var metadata in bulkset)
                {
                    FileInfo fileInfo = new  FileInfo(metadata.Path);

                    if (!fileInfo.Exists)
                    {
                        Console.WriteLine($"File does not exist '{metadata.Path}'");
                        continue;
                    }
                    Track track = new Track(metadata.Path);
                    var metadataInfo = await _fileMetaDataService.GetMetadataInfoAsync(fileInfo);

                    string artistsValue = _mediaTagWriteService.GetDictionaryValue(track, "artists");

                    //double checking the file here if the artist is truly missing
                    //maybe the database is not up to date
                    if (Fuzz.PartialRatio(artistsValue.ToLower(), addArtist.ToLower()) > FuzzyMatchRatio)
                    {
                        continue;
                    }
                    
                    if (string.IsNullOrWhiteSpace(artistsValue))
                    {
                        artistsValue = addArtist;
                    }
                    else if (artistsValue.Length > 0)
                    {
                        if (!artistsValue.EndsWith(";"))
                        {
                            artistsValue += ";";
                        }

                        artistsValue += addArtist;
                    }
                    
                    bool trackInfoUpdated = false;
                    _mediaTagWriteService.UpdateTag(track, metadataInfo, "artists", artistsValue, ref trackInfoUpdated, true);
                    if (trackInfoUpdated)
                    {
                        Console.WriteLine($"Setting tag of file '{metadata.Path}'");
                        updatedTracks.Add(track);
                    }
                }
                
                Console.WriteLine("Save the changes ? (y/n)");
                if (confirm || Console.ReadLine()?.ToLower() == "y")
                {
                    foreach (Track track in updatedTracks)
                    {
                        string filePath = track.Path;
                        Console.WriteLine($"Saving file '{track.Path}'");
                        if (await _mediaTagWriteService.SafeSaveAsync(track))
                        {
                            await _importCommandHandler.ProcessFileAsync(filePath);
                        }
                    }
                }
                offset += BulkProcess;
            }
    }
}