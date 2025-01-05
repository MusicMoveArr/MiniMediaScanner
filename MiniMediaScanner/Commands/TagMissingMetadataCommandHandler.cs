using System.Diagnostics;
using MiniMediaScanner.Models;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class TagMissingMetadataCommandHandler
{
    private readonly AcoustIdService _acoustIdService;
    private readonly MusicBrainzAPIService _musicBrainzAPIService;
    private readonly DatabaseService _databaseService;
    private Stopwatch sw = Stopwatch.StartNew();
    
    public TagMissingMetadataCommandHandler(string connectionString)
    {
        _databaseService = new DatabaseService(connectionString);
        _acoustIdService = new AcoustIdService();
        _musicBrainzAPIService = new MusicBrainzAPIService();
    }
    
    public void FingerPrintMedia(string accoustId, bool writeToFile)
    {
        const int limit = 1000;
        int offset = 0;
        
        while (true)
        {
            var metadata = _databaseService.GetMissingMusicBrainzMetadataRecords(offset, limit);
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