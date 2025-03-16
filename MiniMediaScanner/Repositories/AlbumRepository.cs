using Npgsql;
using Dapper;

namespace MiniMediaScanner.Repositories;

public class AlbumRepository
{
    private readonly string _connectionString;
    public AlbumRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<Guid> InsertOrFindAlbumAsync(string albumName, Guid artistId)
    {
        string query = @"
            INSERT INTO Albums (AlbumId, Title, ArtistId) 
            VALUES (@id, @title, @artistId) 
            ON CONFLICT (Title, ArtistId) 
            DO UPDATE SET Title = EXCLUDED.Title 
            RETURNING AlbumId";
        
        Guid albumId = Guid.NewGuid();
        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.ExecuteScalarAsync<Guid>(query, new
        {
            id = albumId,
            title = albumName,
            artistId = artistId
        });
    }
}