using System.Text.RegularExpressions;
using FuzzySharp;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class DeDuplicateFileCommandHandler
{
    private readonly ArtistRepository _artistRepository;
    private readonly MetadataRepository _metadataRepository;
    private readonly FingerPrintService _fingerprintService;
    
    public bool Delete { get; set; }
    public int Accuracy { get; set; }
    public double AcoustFingerprintAccuracy { get; set; }
    public List<string> Extensions { get; set; }
    public bool CheckExtensions { get; set; }
    public bool CheckVersions { get; set; }
    public bool CheckAlbumDuplicates { get; set; }
    public bool CheckAlbumExtensions{ get; set; }
    public bool CheckAlbumExtensionsAcoustFingerprint{ get; set; }
    public static int DeduplicateCount = 0;

    public DeDuplicateFileCommandHandler(string connectionString)
    {
        _artistRepository = new ArtistRepository(connectionString);
        _metadataRepository = new MetadataRepository(connectionString);
        _fingerprintService = new FingerPrintService(); 
    }

    public async Task CheckDuplicateFilesAsync()
    {
        await ParallelHelper.ForEachAsync(await _artistRepository.GetAllArtistNamesLowercaseUniqueAsync(), 4, async artist =>
        {
            try
            {
                await CheckDuplicateFilesAsync(artist);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        });
    }
    
    public async Task CheckDuplicateFilesAsync(string artistName)
    {
        Console.WriteLine($"Checking artist '{artistName}'");
        try
        {
            if (CheckExtensions)
            {
                await FindDuplicateFileExtensionsAsync(artistName);
            }
            if (CheckAlbumExtensions)
            {
                await FindDuplicateAlbumFileExtensionsAsync(artistName);
            }

            if (CheckVersions)
            {
                await FindDuplicateFileVersionsAsync(artistName);
            }

            if (CheckAlbumDuplicates)
            {
                await FindDuplicateAlbumFileNamesAsync(artistName);
            }
            
            if (CheckAlbumExtensionsAcoustFingerprint)
            {
                await FindDuplicateAlbumFileExtensionsAcoustFingerprintAsync(artistName);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
    
    private async Task FindDuplicateAlbumFileExtensionsAcoustFingerprintAsync(string artistName)
    {
        var groupedFiles = (await _metadataRepository.GetMetadataByArtistAsync(artistName))
            .GroupBy(group =>
                new
                {
                    AlbumTitle = group.AlbumName.ToLower()
                });
        
        foreach (var albumFiles in groupedFiles)
        {
            var existingFiles = albumFiles
                .Where(track => !string.IsNullOrWhiteSpace(track.Tag_AcoustIdFingerprint))
                .Where(f => File.Exists(f.Path))
                .AsParallel()
                .Select(track => new
                {
                    Track = track,
                    Decoded = _fingerprintService.DecodeAcoustIdFingerprint(track.Tag_AcoustIdFingerprint!)
                })
                .ToList();

            var similarTracks = existingFiles
                .AsParallel()
                .Select(track => new
                {
                    Track = track,
                    Similarities = existingFiles
                        .Where(f =>  f.Track.MetadataId != track.Track.MetadataId)
                        .Where(f => Math.Abs((f.Track.TrackLength - track.Track.TrackLength).TotalSeconds) < 5)
                        .AsParallel()
                        .Select(f => new
                        {
                            Track = f,
                            Similar = _fingerprintService.DTWSimilarity(f.Decoded, track.Decoded)
                        })
                        .Where(x => x.Similar > AcoustFingerprintAccuracy)
                        .ToList()
                    
                })
                .Where(x => x.Similarities.Any())
                .ToList();

            List<Guid> alreadyProcessed = new List<Guid>();

            foreach (var similarTrack in similarTracks)
            {
                var tracks = new List<MetadataModel>();
                tracks.Add(similarTrack.Track.Track);
                tracks.AddRange(similarTrack.Similarities.Select(t => t.Track.Track));

                tracks = tracks
                    .Where(t => File.Exists(t.Path))
                    .Select(t => new
                    {
                        Track = t,
                        Info = new FileInfo(t.Path)
                    })
                    .OrderByDescending(t => t.Info.Length)
                    .ThenByDescending(t => t.Info.Name.Length)
                    .Select(t => t.Track)
                    .ToList();

                if (tracks.Count <= 1)
                {
                    continue;
                }
                
                MetadataModel recordToKeep = null;
                foreach (string extension in Extensions)
                {
                    var record = tracks
                        .FirstOrDefault(path => path.Path.EndsWith(extension));
                
                    if (record != null)
                    {
                        recordToKeep = record;
                        break;
                    }
                }
            
                if (recordToKeep == null || alreadyProcessed.Contains(recordToKeep.MetadataId.Value))
                {
                    continue;
                }
                
                var toRemove = tracks
                    .Where(file => !string.Equals(recordToKeep.Path, file.Path))
                    .ToList();

                if (toRemove.Count == 0)
                {
                    continue;
                }

                Console.WriteLine($"Keeping file {recordToKeep.Path}");
                foreach (var file in toRemove)
                {
                    var diff = file.TrackLength - recordToKeep.TrackLength;
                    var diff2 = recordToKeep.TrackLength - file.TrackLength;

                    if (diff.TotalSeconds > 5)
                    {
                        continue;
                    }
                    
                    if (Delete)
                    {
                        Console.WriteLine($"Delete duplicate file, {file.Path}");
                        File.Delete(file.Path);
                        await _metadataRepository.DeleteMetadataRecordsAsync([file.MetadataId.ToString()]);
                    }
                    else
                    {
                        DeduplicateCount++;
                        Console.WriteLine($"Duplicate file, {file.Path}");
                    }
                }
                Console.WriteLine();
                alreadyProcessed.Add(recordToKeep.MetadataId.Value);
            }
        }
        Console.WriteLine($"duplicate delete count: {DeduplicateCount}");
    }

    private async Task FindDuplicateAlbumFileExtensionsAsync(string artistName)
    {
        var duplicateFiles = (await _metadataRepository.GetDuplicateAlbumFileExtensionsAsync(artistName))
            .GroupBy(group =>
                new
                {
                    AlbumTitle = group.AlbumTitle.ToLower(),
                    Tracktitle = group.Title.ToLower()
                });
        
        foreach (var albumDuplicates in duplicateFiles)
        {
            DuplicateAlbumFileNameModel recordToKeep = null;

            foreach (string extension in Extensions)
            {
                var record = albumDuplicates
                    .FirstOrDefault(path => path.Path.EndsWith(extension) && new FileInfo(path.Path).Exists);
                
                if (record != null)
                {
                    recordToKeep = record;
                    break;
                }
            }
            
            if (recordToKeep == null)
            {
                continue;
            }

            var toRemove = albumDuplicates
                .Where(file => !string.Equals(recordToKeep.Path, file.Path))
                .Where(file => new FileInfo(file.Path).Exists)
                .ToList();

            if (toRemove.Count == 0)
            {
                continue;
            }

            Console.WriteLine($"Keeping file {recordToKeep.Path}");
            foreach (var file in toRemove)
            {
                if (Delete)
                {
                    Console.WriteLine($"Delete duplicate file, Title '{albumDuplicates.Key.Tracktitle}', {file.Path}");
                    new FileInfo(file.Path).Delete();
                    await _metadataRepository.DeleteMetadataRecordsAsync(new List<string>(new string[] { file.MetadataId.ToString() }));
                }
                else
                {
                    Console.WriteLine($"Duplicate file, Title '{albumDuplicates.Key.Tracktitle}', {file.Path}");
                }
            }
            Console.WriteLine($"");
        }
    }
    
    
    private async Task FindDuplicateAlbumFileNamesAsync(string artistName)
    {
        var duplicateFiles = (await _metadataRepository.GetDuplicateAlbumFileNamesAsync(artistName, Accuracy))
            .GroupBy(group =>
                new
                {
                    group.AlbumId
                });
        
        foreach (var albumDuplicates in duplicateFiles)
        {
            var fileGroups = new List<List<DuplicateAlbumFileNameModel>>();
            foreach (var file in albumDuplicates)
            {
                var matchingGroup = fileGroups
                    .FirstOrDefault(group => group.Any(n => Fuzz.Ratio(file.FileName, n.FileName) >= Accuracy));

                if (matchingGroup != null)
                {
                    matchingGroup.Add(file); // Add to the found group
                }
                else
                {
                    fileGroups.Add(new List<DuplicateAlbumFileNameModel> { file }); // Create a new group
                }
            }

            foreach (var duplicateFileVersions in fileGroups)
            {
                DuplicateAlbumFileNameModel recordToKeep = null;

                foreach (string extension in Extensions)
                {
                    var record = duplicateFileVersions
                        .FirstOrDefault(path => new FileInfo($"{path.Path.Substring(0, path.Path.LastIndexOf('.'))}.{extension}").Exists);

                    if (record != null)
                    {
                        recordToKeep = record;
                        break;
                    }
                }
                
                if (recordToKeep == null)
                {
                    continue;
                }

                var toRemove = duplicateFileVersions
                    .Where(file => !string.Equals(recordToKeep.Path, file.Path))
                    .Where(file => new FileInfo(file.Path).Exists)
                    .ToList();

                if (toRemove.Count == 0)
                {
                    continue;
                }

                Console.WriteLine($"Keeping file {recordToKeep.Path}");
                foreach (var file in toRemove)
                {
                    if (Delete)
                    {
                        Console.WriteLine($"Delete duplicate file {file.Path}");
                        new FileInfo(file.Path).Delete();
                        await _metadataRepository.DeleteMetadataRecordsAsync(new List<string>(new string[] { file.MetadataId.ToString() }));
                    }
                    else
                    {
                        Console.WriteLine($"Duplicate file {file.Path}");
                    }
                }
                Console.WriteLine();
            }
        }
    }
    
    private async Task FindDuplicateFileExtensionsAsync(string artistName)
    {
        var duplicateFiles = (await _metadataRepository.GetDuplicateFileExtensionsAsync(artistName))
            .GroupBy(group => group.FilePathWithoutExtension);

        foreach (var duplicateFileVersions in duplicateFiles)
        {
            var fileWithoutExtension = duplicateFileVersions.First().FilePathWithoutExtension;
            var recordToKeep = (await Task.WhenAll(
                    Extensions
                        .Select(async ext =>
                            (await _metadataRepository.GetMetadataByPathAsync(fileWithoutExtension + "." + ext))
                            .FirstOrDefault())
                ))
                .Where(metadata => metadata != null)
                .FirstOrDefault(metadata => new FileInfo(metadata.Path).Exists);
            
            if (recordToKeep == null)
            {
                continue;
            }

            var toRemove = duplicateFileVersions
                .Where(file => !string.Equals(recordToKeep.Path, file.Path))
                .Where(file => new FileInfo(file.Path).Exists)
                .ToList();

            if (toRemove.Count == 0)
            {
                continue;
            }

            Console.WriteLine($"Keeping file {recordToKeep.Path}");
            foreach (var file in toRemove)
            {
                if (Delete)
                {
                    Console.WriteLine($"Delete duplicate file {file.Path}");
                    new FileInfo(file.Path).Delete();
                    await _metadataRepository.DeleteMetadataRecordsAsync(new List<string>(new string[] { file.MetadataId.ToString() }));
                }
                else
                {
                    Console.WriteLine($"Duplicate file {file.Path}");
                }
            }
            Console.WriteLine($"");
        }
    }
    
    private async Task FindDuplicateFileVersionsAsync(string artistName)
    {
        //regex for files ending with (1).flac, (2).mp3 etc
        string regexFilter = @" \([0-9]*\)(?=\.([a-zA-Z0-9]{2,5})$)";
        List<MetadataModel> possibleDuplicateFiles = await _metadataRepository.GetDuplicateFileVersionsAsync(artistName);

        foreach (MetadataModel possibleDuplicateFile in possibleDuplicateFiles)
        {
            string nonDuplicateFile = Regex.Replace(possibleDuplicateFile.Path, regexFilter, string.Empty);
            MetadataModel? nonDuplicateRecord = (await _metadataRepository
                .GetMetadataByPathAsync(nonDuplicateFile))
                .FirstOrDefault();

            //try to find another non-duplicate version, different media extension
            if (nonDuplicateRecord == null)
            {
                string extension = Path.GetExtension(nonDuplicateFile).ToLower().Replace(".", "");
                string fileWithoutExtension = Path.ChangeExtension(nonDuplicateFile, "");
                
                nonDuplicateRecord = (await Task.WhenAll(
                        Extensions
                            .Where(ext => ext != extension)
                            .Select(ext => fileWithoutExtension + ext)
                            .Select(async path =>
                                (await _metadataRepository.GetMetadataByPathAsync(path))
                                .FirstOrDefault())
                    ))
                    .FirstOrDefault(metadata => metadata != null);

                if (!string.IsNullOrWhiteSpace(nonDuplicateRecord?.Path))
                {
                    nonDuplicateFile = nonDuplicateRecord.Path;
                }
            }
            
            FileInfo nonDuplicatefileInfo = new FileInfo(nonDuplicateFile);
            FileInfo duplicatefileInfo = new FileInfo(possibleDuplicateFile.Path);
            if (!nonDuplicatefileInfo.Exists  ||
                !duplicatefileInfo.Exists ||
                nonDuplicateRecord == null)
            {
                continue;
            }

            if (!string.Equals(nonDuplicateRecord.Title, possibleDuplicateFile.Title, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(nonDuplicateRecord.AlbumId.ToString(), possibleDuplicateFile.AlbumId.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Console.WriteLine($"Keeping file {nonDuplicateRecord.Path}");
            if (Delete)
            {
                Console.WriteLine($"Delete duplicate file {possibleDuplicateFile.Path}");
                duplicatefileInfo.Delete();
                await _metadataRepository.DeleteMetadataRecordsAsync(new List<string>(new string[] { possibleDuplicateFile.MetadataId.ToString() }));
            }
            else
            {
                Console.WriteLine($"Duplicate file {possibleDuplicateFile.Path}");
            }
            Console.WriteLine($"");
        }
    }
}