using Dapper;
using Npgsql;

namespace MiniMediaScanner.Repositories;

public class MusicBrainzReleaseArtistRepository
{
    private readonly string _connectionString;
    public MusicBrainzReleaseArtistRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<Guid> InsertMusicBrainzReleaseTrackArtistAsync(
        Guid musicBrainzReleaseTrackId, 
        Guid artistId, 
        string? joinPhrase,
        int index)
    {
        if (string.IsNullOrWhiteSpace(joinPhrase))
        {
            joinPhrase = string.Empty;
        }
        
        string query = @"INSERT INTO musicbrainz_release_track_artist (musicbrainzreleasetrackid, 
                               musicbrainzartistid, 
                               JoinPhrase, 
                               index)
                         VALUES (@musicBrainzReleaseTrackId, @artistId, @joinPhrase, @index)
                         ON CONFLICT (MusicBrainzReleaseTrackId, musicbrainzartistid) 
                         DO UPDATE SET 
                             JoinPhrase = EXCLUDED.JoinPhrase, 
                             index = EXCLUDED.index";
        
        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.ExecuteScalarAsync<Guid>(query, new
            {
                musicBrainzReleaseTrackId,
                artistId, 
                joinPhrase,
                index
            });
    }
}