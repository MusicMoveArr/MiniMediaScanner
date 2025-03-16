using Dapper;
using Npgsql;

namespace MiniMediaScanner.Repositories;

public class StatsRepository
{
    private readonly string _connectionString;
    public StatsRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<int> GetGenericCountAsync(string tableName)
    {
        string query = $"SELECT count(*) FROM {tableName}";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<int>(query);
    }
    
    public async Task<int> GetTracksAddedCountAsync(int lastDays)
    {
        string query = @$"SELECT count(*) 
                         FROM metadata 
                         WHERE file_creationtime > NOW() - INTERVAL '{lastDays} days';";
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<int>(query);
    }
}