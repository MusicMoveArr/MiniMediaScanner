using Dapper;
using Npgsql;

namespace MiniMediaScanner.Repositories;

public class MusicBrainzReleaseArtistRepository
{
    private readonly string _connectionString;
    public MusicBrainzReleaseArtistRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<Guid> UpsertReleaseTrackArtistAsync(
        Guid releaseTrackId, 
        Guid artistId, 
        string? joinPhrase,
        int index)
    {
        if (string.IsNullOrWhiteSpace(joinPhrase))
        {
            joinPhrase = string.Empty;
        }
        
        string query = @"INSERT INTO MusicBrainz_Release_Track_Artist (releasetrackid, 
                               artistid, 
                               JoinPhrase, 
                               index)
                         VALUES (@releaseTrackId, @artistId, @joinPhrase, @index)
                         ON CONFLICT (ReleaseTrackId, artistid) 
                         DO UPDATE SET 
                             JoinPhrase = EXCLUDED.JoinPhrase, 
                             index = EXCLUDED.index";
        
        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.ExecuteScalarAsync<Guid>(query, new
            {
                releaseTrackId,
                artistId, 
                joinPhrase,
                index
            });
    }
}