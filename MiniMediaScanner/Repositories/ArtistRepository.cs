using Npgsql;

namespace MiniMediaScanner.Repositories;

public class ArtistRepository
{
    private readonly string _connectionString;
    public ArtistRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public List<string> GetAllArtistNames()
    {
        string query = @"SELECT name FROM artists order by name asc";

        using var conn = new NpgsqlConnection(_connectionString);
        using var cmd = new NpgsqlCommand(query, conn);
        
        conn.Open();

        using var reader = cmd.ExecuteReader();
        
        var result = new List<string>();
        while (reader.Read())
        {
            string artistName = reader.GetString(0);
            if (!string.IsNullOrWhiteSpace(artistName))
            {
                result.Add(artistName);
            }
        }

        return result;
    }
    
    public Guid InsertOrFindArtist(string artistName)
    {
        string query = @"INSERT INTO Artists (ArtistId, Name) VALUES (@id, @name) 
                        ON CONFLICT (Name) 
                        DO UPDATE SET Name=EXCLUDED.Name RETURNING ArtistId";
        Guid artistId = Guid.NewGuid();
        using var conn = new NpgsqlConnection(_connectionString);
        using var cmd = new NpgsqlCommand(query, conn);
        
        conn.Open();
        cmd.Parameters.AddWithValue("id", artistId);
        cmd.Parameters.AddWithValue("name", artistName);

        var result = cmd.ExecuteScalar();
        if (result != null)
        {
            artistId = (Guid)result;
        }

        return artistId;
    }
}