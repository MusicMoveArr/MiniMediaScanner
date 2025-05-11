using Dapper;
using Npgsql;

namespace MiniMediaScanner.Repositories;

public class MusicBrainzLabelRepository
{
    private readonly string _connectionString;
    public MusicBrainzLabelRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task InsertMusicBrainzLabelAsync(
        Guid musicBrainzLabelId, 
        Guid areaId, 
        string name,
        string disambiguation,
        int labelCode,
        string type,
        string lifeSpanBegin,
        string lifeSpanEnd,
        bool lifeSpanEnded,
        string sortName,
        string typeId,
        string country)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            name = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(type))
        {
            type = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(lifeSpanBegin))
        {
            lifeSpanBegin = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(lifeSpanEnd))
        {
            lifeSpanEnd = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(sortName))
        {
            sortName = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(typeId))
        {
            typeId = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(country))
        {
            country = string.Empty;
        }
        
        string query = @"INSERT INTO musicbrainz_label (musicbrainzlabelid, 
                               musicbrainzareaid, 
                               Name, 
                               Disambiguation, 
                               LabelCode, 
                               Type, 
                               LifeSpanBegin, 
                               LifeSpanEnd, 
                               LifeSpanEnded, 
                               SortName, 
                               TypeId, 
                               Country)
                         VALUES (@musicBrainzLabelId, @areaId, @name, @disambiguation, @labelCode, 
                                 @type, @lifeSpanBegin, @lifeSpanEnd, @lifeSpanEnded, 
                                 @sortName, @typeId, @country)
                         ON CONFLICT (musicbrainzlabelid) 
                         DO UPDATE SET 
                             musicbrainzareaid = EXCLUDED.musicbrainzareaid, 
                             Name = EXCLUDED.Name, 
                             Disambiguation = EXCLUDED.Disambiguation, 
                             LabelCode = EXCLUDED.LabelCode, 
                             Type = EXCLUDED.Type, 
                             LifeSpanBegin = EXCLUDED.LifeSpanBegin, 
                             LifeSpanEnd = EXCLUDED.LifeSpanEnd, 
                             LifeSpanEnded = EXCLUDED.LifeSpanEnded, 
                             SortName = EXCLUDED.SortName, 
                             TypeId = EXCLUDED.TypeId, 
                             Country = EXCLUDED.Country";
        
        await using var conn = new NpgsqlConnection(_connectionString);

        await conn.ExecuteAsync(query, new
            {
                musicBrainzLabelId,
                areaId, 
                name,
                disambiguation,
                labelCode,
                type,
                lifeSpanBegin,
                lifeSpanEnd,
                lifeSpanEnded,
                sortName,
                typeId,
                country
            });
    }
    
    public async Task<bool> LabelExistsAsync(Guid labelId)
    {
        string query = @"SELECT 1
                         FROM musicbrainz_label label
                         where label.musicbrainzlabelid = @labelId
                         limit 1";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn
            .ExecuteScalarAsync<int?>(query,
                param: new
                {
                    labelId
                })) == 1;
    }
}