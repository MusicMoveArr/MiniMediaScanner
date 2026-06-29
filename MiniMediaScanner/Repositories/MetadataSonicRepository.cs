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
    
    
    public async Task UpdateMetadataMoodVectorAsync(Guid metadataId)
    {
        string query = @"
            UPDATE Metadata_Mood SET mood_vector = ARRAY[
                (mood_happy->>'happy')::float,
                (mood_happy->>'non_happy')::float,
                (mood_sad->>'sad')::float,
                (mood_sad->>'non_sad')::float,
                (mood_aggressive->>'aggressive')::float,
                (mood_aggressive->>'not_aggressive')::float,
                (mood_relaxed->>'relaxed')::float,
                (mood_relaxed->>'non_relaxed')::float,
                (mood_acoustic->>'acoustic')::float,
                (mood_acoustic->>'non_acoustic')::float,
                (mood_electronic->>'electronic')::float,
                (mood_electronic->>'non_electronic')::float,
                (mood_party->>'party')::float,
                (mood_party->>'non_party')::float,
                (ability_approach->>'approachable')::float,
                (ability_approach->>'moderately approachable')::float,
                (ability_approach->>'not approachable')::float,
                (ability_dance->>'danceable')::float,
                (ability_dance->>'not_danceable')::float,
                (voice_instrumental->>'voice')::float,
                (voice_instrumental->>'instrumental')::float,
                (timbre->>'bright')::float,
                (timbre->>'dark')::float,
                (engagement_3c->>'engaging')::float,
                (engagement_3c->>'moderately engaging')::float,
                (engagement_3c->>'not engaging')::float,
                (engagement_regression->>'engagement')::float,
                (gender->>'male')::float,
                (gender->>'female')::float
            ]::vector where metadataId = @metadataId";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync(query, param: new { metadataId });
    }
    
    
    public async Task<int> GetCountToProcessAsync()
    {
        string query = @"
           select count(metadataid)
           from metadata m
           where not exists (select true
				             from metadata_mood mood
				             where mood.metadataid = m.metadataid)";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QuerySingleAsync<int>(query);
    }
}