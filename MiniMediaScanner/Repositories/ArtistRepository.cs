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
    
    public async Task<List<string>> GetAllArtistNamesAsync()
    {
        string query = @"SELECT name FROM artists order by name asc";
        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn
            .QueryAsync<string>(query))
            .ToList();
    }
    
    public async Task<List<string>> GetArtistNamesCaseInsensitiveAsync(string artistName)
    {
        string query = @"SELECT name FROM artists where LOWER(name) = lower(@artistName)";
        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn
            .QueryAsync<string>(query, param: new { artistName }))
            .ToList();
    }
    
    public async Task<Guid> InsertOrFindArtist(string artistName)
    {
        string query = @"INSERT INTO Artists (ArtistId, Name) VALUES (@id, @name) 
                        ON CONFLICT (Name) 
                        DO UPDATE SET Name=EXCLUDED.Name RETURNING ArtistId";
        Guid artistId = Guid.NewGuid();
        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.ExecuteScalarAsync<Guid>(query, new
        {
            id = artistId,
            name = artistName
        });
    }
}