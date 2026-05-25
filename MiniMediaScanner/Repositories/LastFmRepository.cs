using Dapper;
using MiniMediaScanner.Models.Tidal;
using Npgsql;

namespace MiniMediaScanner.Repositories;

public class LastFmRepository
{
    private readonly string _connectionString;
    public LastFmRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<List<Guid>> GetAllLastFmArtistIdsAsync()
    {
        string query = @"SELECT artistid 
                         FROM lastfm_artist ";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        return (await conn
                .QueryAsync<Guid>(query))
            .ToList();
    }
    
    public async Task<List<string>> GetAllLastFmArtistNamesAsync()
    {
        string query = @"SELECT Name 
                         FROM lastfm_artist ";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        return (await conn
                .QueryAsync<string>(query))
            .ToList();
    }
    
    public async Task<List<string>> GetNonpulledLastFmArtistNamesAsync()
    {
        string query = @"SELECT Name 
                         FROM lastfm_artist 
                         where lastsynctime < '2020-01-01'";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn
                .QueryAsync<string>(query))
            .ToList();
    }
}