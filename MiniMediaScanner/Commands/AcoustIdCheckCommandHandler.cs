using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class AcoustIdCheckCommandHandler
{
    private readonly AcoustIdService _acoustIdService;
    private readonly AcoustIdSubmissionRepository _acoustIdSubmissionRepository;
    public AcoustIdCheckCommandHandler(string connectionString)
    {
        _acoustIdSubmissionRepository = new AcoustIdSubmissionRepository(connectionString);
        _acoustIdService = new AcoustIdService();
    }
    
    public async Task SendSubmissionsAsync(string acoustidClientKey)
    {
        int limit = 500;
        while (true)
        {
            var submissionIds = await _acoustIdSubmissionRepository.GetCheckMetadataIdsAsync();

            if (!submissionIds.Any())
            {
                break;
            }

            Console.WriteLine($"Checking {submissionIds.Count} submissions");

            for (int offset = 0; offset < submissionIds.Count; offset += limit)
            {
                var ids = submissionIds
                    .Skip(offset)
                    .Take(limit)
                    .ToList();
            
                var response = await _acoustIdService.CheckStatusAsync(acoustidClientKey, ids);
                
                foreach (var submission in response.Submissions.Where(s => s.Status != "pending"))
                {
                    await _acoustIdSubmissionRepository.UpdateSubmissionStatusAsync(
                        submission.Id, 
                        submission.Status,
                        submission.Result.Id);
                }
            }
        }
    }
}