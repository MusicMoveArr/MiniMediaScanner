using MiniMediaScanner.Models;
using MiniMediaScanner.Models.MusicBrainzRecordings;
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

    public MusicBrainzArtistModel? GetMusicBrainzArtistByRecordingId(string recordingId)
    {
        
        string query = @"select a.releasecount,
                         a.disambiguation,
                         a.name,
                         cast(a.musicbrainzremoteid as text),
                         a.sortname,
                         a.type,
                         a.name,
                         r.musicbrainzremotereleaseid,
                         r.title,
                         r.status,
                         r.statusid,
                         r.date,
                         r.barcode,
                         r.country,
                         r.disambiguation,
                         r.quality,
                         rt.mediatrackcount,
                         rt.mediaformat,
                         rt.title,
                         rt.position,
                         rt.mediatrackoffset,
                         rt.musicbrainzremotereleasetrackid,
                         rt.title,
                         rt.length,
                         rt.number,
                         rt.position,
                         rt.title,
                         rt.length,
                         rt.recordingvideo,
                         rt.recordingid
 
                         from musicbrainzreleasetrack rt
                         join musicbrainzrelease r on r.musicbrainzremotereleaseid = rt.musicbrainzremotereleaseid
                         join musicbrainzartist a on cast(a.musicbrainzartistid as text) = r.musicbrainzartistid 
                         where rt.recordingid =  @recordingId";

        using var conn = new NpgsqlConnection(_connectionString);
        using var cmd = new NpgsqlCommand(query, conn);
        
        conn.Open();

        cmd.Parameters.AddWithValue("recordingId", recordingId);
        using var reader = cmd.ExecuteReader();
        
        var result = new List<string>();
        if (reader.Read())
        {
            MusicBrainzArtistModel artist = new MusicBrainzArtistModel();
            artist.ReleaseCount = reader.GetInt32(0);
            
            artist.ArtistCredit.Add(new MusicBrainzArtistCreditModel
            {
                Artist = new MusicBrainzArtistCreditEntityModel
                {
                    Disambiguation = reader.GetString(1),
                    Name = reader.GetString(2),
                    Id = reader.GetString(3),
                    SortName = reader.GetString(4),
                    Type = reader.GetString(5),
                }, Name = reader.GetString(6)
            });

            MusicBrainzArtistReleaseModel artistRelease = new MusicBrainzArtistReleaseModel
            {
                Id = reader.GetString(7),
                Title = reader.GetString(8),
                Status = reader.GetString(9),
                StatusId = reader.GetString(10),
                Date = reader.GetString(11),
                Barcode = reader.GetString(12),
                Country = reader.GetString(13),
                Disambiguation = reader.GetString(14),
                Quality = reader.GetString(15),
            };

            MusicBrainzReleaseMediaModel releaseMediaModel = new MusicBrainzReleaseMediaModel
            {
                TrackCount = reader.GetInt32(16),
                Format = reader.GetString(17),
                Title = reader.GetString(18),
                Position = reader.GetInt32(19),
                TrackOffset = reader.GetInt32(20),
            };
            releaseMediaModel.Tracks.Add(new MusicBrainzReleaseMediaTrackModel
            {
                Id = reader.GetString(21),
                Title = reader.GetString(22),
                Length = reader.GetInt32(23),
                Number = reader.GetInt32(24),
                Position = reader.GetInt32(25),
                Recording = new MusicBrainzReleaseMediaTrackRecordingModel
                {
                    Title = reader.GetString(26),
                    Length = reader.GetInt32(27),
                    Video = reader.GetBoolean(28),
                    Id = reader.GetString(29)
                }
            });
            
            artistRelease.Media.Add(releaseMediaModel);
            
            artist.Releases.Add(artistRelease);

            return artist;
        }

        return null;
    }
}