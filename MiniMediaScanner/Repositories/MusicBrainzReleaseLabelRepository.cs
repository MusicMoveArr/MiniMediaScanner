using Dapper;
using Npgsql;

namespace MiniMediaScanner.Repositories;

public class MusicBrainzReleaseLabelRepository
{
    private readonly string _connectionString;
    public MusicBrainzReleaseLabelRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<Guid> UpsertReleaseLabelAsync(
        Guid releaseId, 
        Guid labelId,
        string catalogNumber)
    {
        string query = @"INSERT INTO MusicBrainz_Release_Label (releaseid, labelid, catalognumber)
                         VALUES (@releaseId, @labelId, @catalogNumber)
                         ON CONFLICT (releaseid, labelid, catalognumber) 
                         DO NOTHING";
        
        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.ExecuteScalarAsync<Guid>(query, new
            {
                releaseId,
                labelId,
                catalogNumber
            });
    }
}