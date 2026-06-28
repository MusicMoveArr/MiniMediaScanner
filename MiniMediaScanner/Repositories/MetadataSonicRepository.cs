using System.Text;
using MiniMediaScanner.Models;
using Npgsql;
using Dapper;

namespace MiniMediaScanner.Repositories;

public class MetadataSonicRepository
{
    private readonly string _connectionString;
    public MetadataSonicRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<bool> MetadataMoodExistsAsync(Guid metadataId)
    {
        string query = @"SELECT 1
                         FROM Metadata_Mood
                         where metadataId = @metadataId
                         limit 1";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn
            .ExecuteScalarAsync<int?>(query,
                param: new
                {
                    metadataId
                })) == 1;
    }
    
    public async Task UpsertMetadataMoodAsync(MetadataMoodModel metadataMoodModel)
    {
        string query = @"
            INSERT INTO Metadata_Mood (MetadataId, 
                                  mood_happy, 
                                  mood_sad, 
                                  mood_aggressive, 
                                  mood_relaxed, 
                                  mood_acoustic, 
                                  mood_electronic, 
                                  mood_party, 
                                  ability_approach, 
                                  ability_dance, 
                                  voice_instrumental, 
                                  timbre, 
                                  engagement_3c, 
                                  engagement_regression, 
                                  gender, 
                                  genre_json)
            VALUES (@MetadataId, 
                    @Mood_Happy::jsonb,
                    @Mood_Sad::jsonb,
                    @Mood_Aggressive::jsonb,
                    @Mood_Relaxed::jsonb,
                    @Mood_Acoustic::jsonb,
                    @Mood_Electronic::jsonb,
                    @Mood_Party::jsonb,
                    @Ability_Approach::jsonb,
                    @Ability_Dance::jsonb,
                    @Voice_Instrumental::jsonb,
                    @Timbre::jsonb,
                    @Engagement_3c::jsonb,
                    @Engagement_Regression::jsonb,
                    @Gender::jsonb,
                    @Genre_Json::jsonb)
            ON CONFLICT (MetadataId)
            DO UPDATE SET
                mood_Happy = EXCLUDED.mood_Happy,
                mood_Sad = EXCLUDED.mood_Sad,
                mood_Aggressive = EXCLUDED.mood_Aggressive,
                mood_Relaxed = EXCLUDED.mood_Relaxed,
                mood_Acoustic = EXCLUDED.mood_Acoustic,
                mood_Electronic = EXCLUDED.mood_Electronic,
                ability_Approach = EXCLUDED.ability_Approach,
                ability_Dance = EXCLUDED.ability_Dance,
                voice_Instrumental = EXCLUDED.voice_Instrumental,
                Timbre = EXCLUDED.Timbre,
                Engagement_3c = EXCLUDED.Engagement_3c,
                Engagement_Regression = EXCLUDED.Engagement_Regression,
                Gender = EXCLUDED.Gender,
                Genre_Json = EXCLUDED.Genre_Json";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync(query, metadataMoodModel);
    }
}