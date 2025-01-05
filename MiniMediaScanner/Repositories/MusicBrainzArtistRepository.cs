using Npgsql;
using NpgsqlTypes;

namespace MiniMediaScanner.Repositories;

public class MusicBrainzArtistRepository
{
    private readonly string _connectionString;
    public MusicBrainzArtistRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public List<string> GetMusicBrainzArtistRemoteIdsByName(List<string> names)
    {
        string query = @"SELECT MusicBrainzRemoteId FROM MusicBrainzArtist where name = any(@names)";

        using var conn = new NpgsqlConnection(_connectionString);
        using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("names", NpgsqlDbType.Text | NpgsqlTypes.NpgsqlDbType.Array, names);
        
        conn.Open();

        using var reader = cmd.ExecuteReader();
        
        var result = new List<string>();
        while (reader.Read())
        {
            string artistId = reader.GetString(0);
            if (!string.IsNullOrWhiteSpace(artistId))
            {
                result.Add(artistId);
            }
        }

        return result;
    }
    
    public List<string> GetAllMusicBrainzArtistRemoteIds()
    {
        string query = @"SELECT MusicBrainzRemoteId FROM MusicBrainzArtist order by name asc";

        using var conn = new NpgsqlConnection(_connectionString);
        using var cmd = new NpgsqlCommand(query, conn);
        
        conn.Open();

        using var reader = cmd.ExecuteReader();
        
        var result = new List<string>();
        while (reader.Read())
        {
            string artistId = reader.GetString(0);
            if (!string.IsNullOrWhiteSpace(artistId))
            {
                result.Add(artistId);
            }
        }

        return result;
    }
    
    public List<string> GetMusicBrainzArtistIdsByName(List<string> names)
    {
        string query = @"SELECT MusicBrainzArtistId FROM MusicBrainzArtist where name = any(@names)";

        using var conn = new NpgsqlConnection(_connectionString);
        using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("names", NpgsqlDbType.Text | NpgsqlTypes.NpgsqlDbType.Array, names);
        
        conn.Open();

        using var reader = cmd.ExecuteReader();
        
        var result = new List<string>();
        while (reader.Read())
        {
            string artistId = reader.GetGuid(0).ToString();
            if (!string.IsNullOrWhiteSpace(artistId))
            {
                result.Add(artistId);
            }
        }

        return result;
    }
    
    public List<string> GetAllMusicBrainzArtistIds()
    {
        string query = @"SELECT MusicBrainzArtistId FROM MusicBrainzArtist";

        using var conn = new NpgsqlConnection(_connectionString);
        using var cmd = new NpgsqlCommand(query, conn);
        
        conn.Open();

        using var reader = cmd.ExecuteReader();
        
        var result = new List<string>();
        while (reader.Read())
        {
            string artistId = reader.GetGuid(0).ToString();
            if (!string.IsNullOrWhiteSpace(artistId))
            {
                result.Add(artistId);
            }
        }

        return result;
    }

    public Guid InsertMusicBrainzArtist(string remoteMusicBrainzArtistId, 
        string artistName, 
        string artistType,
        string country,
        string sortName,
        string disambiguation)
    {
        if (string.IsNullOrWhiteSpace(artistType))
        {
            artistType = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(country))
        {
            country = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(sortName))
        {
            sortName = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(disambiguation))
        {
            disambiguation = string.Empty;
        }
        
        string query = @"INSERT INTO MusicBrainzArtist (MusicBrainzArtistId, MusicBrainzRemoteId, Name, Type, Country, SortName, Disambiguation)
                         VALUES (@id, @MusicBrainzRemoteId, @name, @type, @Country, @SortName, @Disambiguation)
                         ON CONFLICT (MusicBrainzRemoteId) 
                         DO UPDATE SET 
                             Name = EXCLUDED.Name, 
                             Type = EXCLUDED.Type, 
                             Country = EXCLUDED.Country, 
                             SortName = EXCLUDED.SortName, 
                             Disambiguation = EXCLUDED.Disambiguation
                         RETURNING MusicBrainzArtistId";
        Guid artistId = Guid.NewGuid();

        using var conn = new NpgsqlConnection(_connectionString);
        using var cmd = new NpgsqlCommand(query, conn);
        
        conn.Open();
        
        cmd.Parameters.AddWithValue("id", artistId);
        cmd.Parameters.AddWithValue("MusicBrainzRemoteId", remoteMusicBrainzArtistId);
        cmd.Parameters.AddWithValue("name", artistName);
        cmd.Parameters.AddWithValue("type", artistType);
        cmd.Parameters.AddWithValue("Country", country);
        cmd.Parameters.AddWithValue("SortName", sortName);
        cmd.Parameters.AddWithValue("Disambiguation", disambiguation);

        var result = cmd.ExecuteScalar();
        if (result != null)
        {
            artistId = (Guid)result;
        }

        return artistId;
    }
    
    public Guid? GetRemoteMusicBrainzArtist(string remoteMusicBrainzArtistId)
    {
        string query = @"SELECT MusicBrainzArtistId FROM MusicBrainzArtist WHERE MusicBrainzRemoteId = @id";

        using var conn = new NpgsqlConnection(_connectionString);
        using var cmd = new NpgsqlCommand(query, conn);
        
        conn.Open();
        
        cmd.Parameters.AddWithValue("id", remoteMusicBrainzArtistId);

        var result = cmd.ExecuteScalar();
        if (!Guid.TryParse(result?.ToString(), out Guid artistId))
        {
            return null;
        }
        return artistId;
    }
}