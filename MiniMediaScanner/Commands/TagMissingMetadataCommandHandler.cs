using System.Diagnostics;
using MiniMediaScanner.Models;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class TagMissingMetadataCommandHandler
{
    private readonly AcoustIdService _acoustIdService;
    private readonly MusicBrainzAPIService _musicBrainzAPIService;
    private Stopwatch sw = Stopwatch.StartNew();
    private readonly MetadataRepository _metadataRepository;
    
    public TagMissingMetadataCommandHandler(string connectionString)
    {
        _acoustIdService = new AcoustIdService();
        _musicBrainzAPIService = new MusicBrainzAPIService();
        _metadataRepository = new MetadataRepository(connectionString);
    }
    
    public void FingerPrintMedia(string accoustId, bool writeToFile)
    {
        const int limit = 1000;
        int offset = 0;
        
        while (true)
        {
            var metadata = _metadataRepository.GetMissingMusicBrainzMetadataRecords(offset, limit);
            offset += limit;

            if (metadata.Count == 0)
            {
                break;
            }
            
            metadata
                .ForEach(metadata =>
                {
                    string recordingId = _acoustIdService.GetMusicBrainzRecordingId(accoustId, metadata.TagAcoustIdFingerPrint, (int)metadata.TagAcoustIdFingerPrintDuration);

                    if (!string.IsNullOrWhiteSpace(recordingId))
                    {
                        var data = _musicBrainzAPIService.GetRecordingById(recordingId);
                    }
                });
        }
    }
}