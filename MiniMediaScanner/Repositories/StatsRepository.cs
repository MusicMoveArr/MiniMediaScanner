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
    
    public int GetGenericCount(string tableName)
    {
        string query = $"SELECT count(*) FROM {tableName}";
        using var conn = new NpgsqlConnection(_connectionString);
        return conn.ExecuteScalar<int>(query);
    }
    
    public int GetTracksAddedCount(int lastDays)
    {
        string query = @$"SELECT count(*) 
                         FROM metadata 
                         WHERE file_creationtime > NOW() - INTERVAL '{lastDays} days';";
        using var conn = new NpgsqlConnection(_connectionString);
        return conn.ExecuteScalar<int>(query);
    }
}