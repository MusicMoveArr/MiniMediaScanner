using Npgsql;

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
        using var cmd = new NpgsqlCommand(query, conn);
        
        conn.Open();
        cmd.Parameters.AddWithValue("id", albumId);
        cmd.Parameters.AddWithValue("title", albumName);
        cmd.Parameters.AddWithValue("artistId", artistId);

        var result = cmd.ExecuteScalar();
        if (result != null)
        {
            albumId = (Guid)result;
        }

        return albumId;
    }
}