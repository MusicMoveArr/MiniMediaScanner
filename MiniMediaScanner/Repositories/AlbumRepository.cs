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
    
    public Guid InsertOrFindAlbum(string albumName, Guid artistId)
    {
        string query = @"
            INSERT INTO Albums (AlbumId, Title, ArtistId) 
            VALUES (@id, @title, @artistId) 
            ON CONFLICT (Title, ArtistId) 
            DO UPDATE SET Title = EXCLUDED.Title 
            RETURNING AlbumId";
        
        Guid albumId = Guid.NewGuid();
        using var conn = new NpgsqlConnection(_connectionString);

        return conn.ExecuteScalar<Guid>(query, new
        {
            id = albumId,
            title = albumName,
            artistId = artistId
        });
    }
}