using Dapper;
using Npgsql;

namespace MiniMediaScanner.Repositories;

public class DiscogsRepository
{
    private readonly string _connectionString;
    public DiscogsRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<int?> GetAlbumIdByNameAsync(int artistId, string albumName)
    {
        string query = @"select album.ReleaseId
                         from discogs_release album
                         join discogs_release_artist dra on dra.releaseid = album.releaseid  and dra.artistid = @artistId
                         where lower(album.Title) = lower(@albumName)
                         limit 1";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        return await conn
                .QueryFirstOrDefaultAsync<int>(query,
                    param: new
                    {
                        artistId,
                        albumName
                    });
    }
}