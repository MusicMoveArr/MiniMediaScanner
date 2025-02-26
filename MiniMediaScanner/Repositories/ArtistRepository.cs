using Npgsql;
using Dapper;

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

        return conn
            .Query<string>(query)
            .ToList();
    }
    
    public List<string> GetArtistNamesCaseInsensitive(string artistName)
    {
        string query = @"SELECT name FROM artists where LOWER(name) = lower(@artistName)";
        using var conn = new NpgsqlConnection(_connectionString);

        return conn
            .Query<string>(query, param: new { artistName })
            .ToList();
    }
    
    public Guid InsertOrFindArtist(string artistName)
    {
        string query = @"INSERT INTO Artists (ArtistId, Name) VALUES (@id, @name) 
                        ON CONFLICT (Name) 
                        DO UPDATE SET Name=EXCLUDED.Name RETURNING ArtistId";
        Guid artistId = Guid.NewGuid();
        using var conn = new NpgsqlConnection(_connectionString);

        return conn.ExecuteScalar<Guid>(query, new
        {
            id = artistId,
            name = artistName
        });
    }
}