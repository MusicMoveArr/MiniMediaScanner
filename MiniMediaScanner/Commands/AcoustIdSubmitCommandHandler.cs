using DapperBulkQueries.Common;
using DapperBulkQueries.Npgsql;
using MiniMediaScanner.Models.AcoustId;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;
using Npgsql;

namespace MiniMediaScanner.Commands;

public class AcoustIdSubmitCommandHandler
{
    private readonly AcoustIdService _acoustIdService;
    private readonly AcoustIdSubmissionRepository _acoustIdSubmissionRepository;
    private readonly string _connectionString;
    public AcoustIdSubmitCommandHandler(string connectionString)
    {
        _connectionString = connectionString;
        _acoustIdSubmissionRepository = new AcoustIdSubmissionRepository(connectionString);
        _acoustIdService = new AcoustIdService();
    }
    
    public async Task SendSubmissionsAsync(string acoustidClientKey, string acoustidUserKey)
    {
        List<AcoustIdSubmissionDto> bulkInsert = new List<AcoustIdSubmissionDto>();
        List<string> columns = new()
        {
            "SubmissionId", 
            "MetadataId", 
            "Status", 
            "SubmittedAt"
        };
        
        int limit = 50;
        while (true)
        {
            var metadataFiles = await _acoustIdSubmissionRepository.GetMetadataNotSubmittedAsync();

            if (!metadataFiles.Any())
            {
                break;
            }

            Console.WriteLine($"Sending {metadataFiles.Count} fingerprints");

            for (int offset = 0; offset < metadataFiles.Count; offset += limit)
            {
                var tracks = metadataFiles
                    .Skip(offset)
                    .Take(limit)
                    .ToList();
            
                var response = await _acoustIdService.SubmitAsync(acoustidClientKey, acoustidUserKey, tracks);
                
                foreach (var submission in response.Submissions)
                {
                    var trackIndex = tracks[int.Parse(submission.Index)];
                    
                    bulkInsert.Add(new AcoustIdSubmissionDto
                    {
                        MetadataId = trackIndex.MetadataId.Value,
                        Status = submission.Status,
                        SubmissionId = submission.Id,
                        SubmittedAt = DateTime.Now
                    });
                }
                
                if (bulkInsert.Count > 1000)
                {
                    await using var conn = new NpgsqlConnection(_connectionString);
                    await conn.ExecuteBulkInsertAsync(
                        "acoustid_submission",
                        bulkInsert,
                        columns,
                        onConflict: OnConflict.DoNothing);
                    bulkInsert.Clear();
                }
            }

        }
        
        if (bulkInsert.Count > 0)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.ExecuteBulkInsertAsync(
                "acoustid_submission",
                bulkInsert,
                columns,
                onConflict: OnConflict.DoNothing);
            bulkInsert.Clear();
        }
    }
}