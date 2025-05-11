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
    
    public async Task<Guid> InsertMusicBrainzReleaseLabelAsync(
        Guid musicBrainzReleaseId, 
        Guid labelId)
    {
        string query = @"INSERT INTO musicbrainz_release_label (musicbrainzreleaseid, musicbrainzlabelid)
                         VALUES (@musicBrainzReleaseId, @labelId)
                         ON CONFLICT (musicbrainzreleaseid, musicbrainzlabelid) 
                         DO NOTHING";
        
        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.ExecuteScalarAsync<Guid>(query, new
            {
                musicBrainzReleaseId,
                labelId
            });
    }
}