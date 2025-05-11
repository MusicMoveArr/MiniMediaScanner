using Dapper;
using Npgsql;

namespace MiniMediaScanner.Repositories;

public class MusicBrainzAreaRepository
{
    private readonly string _connectionString;
    public MusicBrainzAreaRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<Guid> InsertMusicBrainzAreaAsync(
        Guid areaId, 
        string name,
        string? type,
        string? typeId,
        string? sortName,
        string? disambiguation)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            type = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(typeId))
        {
            typeId = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(sortName))
        {
            sortName = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(disambiguation))
        {
            disambiguation = string.Empty;
        }
        
        string query = @"INSERT INTO musicbrainz_area (musicbrainzareaid, 
                               Name, Type, TypeId,
                               SortName, Disambiguation)
                         VALUES (@areaId, @name, @type,
                                 @typeId, @sortName, @disambiguation)
                         ON CONFLICT (musicbrainzareaid) 
                         DO UPDATE SET 
                             Name = EXCLUDED.Name, 
                             Type = EXCLUDED.Type, 
                             TypeId = EXCLUDED.TypeId, 
                             SortName = EXCLUDED.SortName, 
                             Disambiguation = EXCLUDED.Disambiguation";
        
        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.ExecuteScalarAsync<Guid>(query, new
            {
                areaId,
                name,
                type,
                typeId,
                sortName,
                disambiguation
            });
    }
}