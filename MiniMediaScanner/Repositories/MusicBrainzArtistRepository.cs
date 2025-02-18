using Dapper;
using MiniMediaScanner.Models.MusicBrainz;
using MiniMediaScanner.Models.MusicBrainz.MusicBrainzRecordings;
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
    
    public List<string> GetMusicBrainzArtistRemoteIdsByName(string artist)
    {
        string query = @"SELECT MusicBrainzRemoteId FROM MusicBrainzArtist where lower(name) = lower(@artist)";

        using var conn = new NpgsqlConnection(_connectionString);

        return conn
            .Query<string>(query, new
            {
                artist
            })
            .ToList();
    }
    
    public List<string> GetAllMusicBrainzArtistRemoteIds()
    {
        string query = @"SELECT MusicBrainzRemoteId FROM MusicBrainzArtist order by name asc";

        using var conn = new NpgsqlConnection(_connectionString);
        
        return conn
            .Query<string>(query)
            .ToList();
    }
    
    public List<Guid> GetMusicBrainzArtistIdsByName(string artistName)
    {
        string query = @"SELECT MusicBrainzArtistId FROM MusicBrainzArtist where lower(name) = lower(@artistName)";

        using var conn = new NpgsqlConnection(_connectionString);

        return conn
            .Query<Guid>(query, new
            {
                artistName
            }).ToList();
    }
    
    public List<Guid> GetAllMusicBrainzArtistIds()
    {
        string query = @"SELECT MusicBrainzArtistId FROM MusicBrainzArtist";

        using var conn = new NpgsqlConnection(_connectionString);

        return conn
            .Query<Guid>(query)
            .ToList();
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
        
        string query = @"INSERT INTO MusicBrainzArtist (MusicBrainzArtistId, 
                               MusicBrainzRemoteId, Name, Type, Country, SortName, Disambiguation)
                         VALUES (@id, @MusicBrainzRemoteId, @name, @type, @Country, @SortName, @Disambiguation)
                         ON CONFLICT (MusicBrainzRemoteId) 
                         DO UPDATE SET 
                             Name = EXCLUDED.Name, 
                             Type = EXCLUDED.Type, 
                             Country = EXCLUDED.Country, 
                             SortName = EXCLUDED.SortName, 
                             Disambiguation = EXCLUDED.Disambiguation,
                             lastsynctime = current_timestamp
                         RETURNING MusicBrainzArtistId";
        Guid artistId = Guid.NewGuid();
        using var conn = new NpgsqlConnection(_connectionString);

        return conn.ExecuteScalar<Guid>(query, new
            {
                id = artistId,
                MusicBrainzRemoteId = remoteMusicBrainzArtistId,
                name = artistName,
                type = artistType,
                Country = country,
                SortName = sortName,
                Disambiguation = disambiguation
            });
    }
    
    public Guid? GetRemoteMusicBrainzArtistId(string remoteMusicBrainzArtistId)
    {
        string query = @"SELECT MusicBrainzArtistId FROM MusicBrainzArtist WHERE MusicBrainzRemoteId = @id";

        using var conn = new NpgsqlConnection(_connectionString);

        return conn.ExecuteScalar<Guid?>(query, new
            {
                id = remoteMusicBrainzArtistId
            });
    }
    public DateTime GetBrainzArtistLastSyncTime(string remoteMusicBrainzArtistId)
    {
        string query = @"SELECT lastsynctime FROM MusicBrainzArtist WHERE MusicBrainzRemoteId = @id";

        using var conn = new NpgsqlConnection(_connectionString);

        return conn.ExecuteScalar<DateTime>(query, new
        {
            id = remoteMusicBrainzArtistId
        });
    }


    public MusicBrainzArtistModel? GetMusicBrainzArtistByRecordingId(string recordingId)
    {
        const string query = @"
                                SELECT 
                                    cast(a.musicbrainzremoteid as text) AS ArtistMusicBrainzRemoteId,
                                    a.releasecount AS ArtistReleaseCount,
                                    a.disambiguation AS ArtistDisambiguation,
                                    a.name AS ArtistName,
                                    a.sortname AS ArtistSortName,
                                    a.type AS ArtistType,
                                    r.musicbrainzremotereleaseid AS ReleaseMusicBrainzRemoteReleaseId,
                                    r.title AS ReleaseTitle,
                                    r.status AS ReleaseStatus,
                                    r.statusid AS ReleaseStatusId,
                                    r.date AS ReleaseDate,
                                    r.barcode AS ReleaseBarcode,
                                    r.country AS ReleaseCountry,
                                    r.disambiguation AS ReleaseDisambiguation,
                                    r.quality AS ReleaseQuality,
                                    rt.recordingid AS ReleaseTrackRecordingId,
                                    rt.musicbrainzremotereleasetrackid AS ReleaseTrackMusicBrainzRemoteReleaseTrackId,
                                    rt.mediatrackcount AS ReleaseTrackMediaTrackCount,
                                    rt.mediaformat AS ReleaseTrackMediaFormat,
                                    rt.title AS ReleaseTrackTitle,
                                    rt.position AS ReleaseTrackPosition,
                                    rt.mediatrackoffset AS ReleaseTrackMediaTrackOffset,
                                    rt.length AS ReleaseTrackLength,
                                    rt.number AS ReleaseTrackNumber,
                                    rt.recordingvideo AS ReleaseTrackRecordingVideo
                                FROM musicbrainzreleasetrack rt
                                 join musicbrainzrelease r on r.musicbrainzremotereleaseid = rt.musicbrainzremotereleaseid
                                 join musicbrainzartist a on cast(a.musicbrainzartistid as text) = r.musicbrainzartistid 
                                WHERE rt.recordingid = @recordingId";

        var lookup = new Dictionary<string, MusicBrainzArtistModel>();

        using var conn = new NpgsqlConnection(_connectionString);
        var records = conn.Query<MusicBrainzRecordingFlatModel>(query, 
            param: new
            {
                recordingId = recordingId
            });

        MusicBrainzArtistModel artistModel = new MusicBrainzArtistModel();

        artistModel.ArtistCredit = records
            .GroupBy(record => record.ArtistMusicBrainzRemoteId)
            .Select(record => record.First())
            .Select(record => new MusicBrainzArtistCreditModel
            {
                Name = record.ArtistName,
                Artist = new MusicBrainzArtistCreditEntityModel
                {
                    Disambiguation = record.ArtistDisambiguation,
                    Name = record.ArtistName,
                    Type = record.ArtistType,
                    Id = record.ArtistMusicBrainzRemoteId,
                    SortName = record.ArtistSortName
                }
                
            })
            .ToList();
        
        artistModel.Releases = records
            .GroupBy(record => record.ReleaseMusicBrainzRemoteReleaseId)
            .Select(record => record.First())
            .Select(record => new MusicBrainzArtistReleaseModel
            {
                Id = record.ReleaseMusicBrainzRemoteReleaseId,
                Title = record.ReleaseTitle,
                Status = record.ReleaseStatus,
                StatusId = record.ReleaseStatusId,
                Date = record.ReleaseDate.ToString("yyyy-MM-dd") ?? string.Empty,
                Barcode = record.ReleaseBarcode,
                Country = record.ReleaseCountry,
                Disambiguation = record.ReleaseDisambiguation,
                Quality = record.ReleaseQuality,
                TextRepresentation = new MusicBrainzTextRepresentationModel
                {
                    Language = string.Empty,
                    Script = string.Empty,
                }
            })
            .ToList();
        
        var releaseTracks = records
            .GroupBy(record => record.ReleaseTrackRecordingId)
            .Select(record => record.First())
            .Select(record => new MusicBrainzReleaseMediaTrackModel
            {
                Id = record.ReleaseTrackMusicBrainzRemoteReleaseTrackId,
                Title = record.ReleaseTrackTitle,
                Position = record.ReleaseTrackPosition,
                Length = record.ReleaseTrackLength,
                Number = record.ReleaseTrackNumber,
                Recording = new MusicBrainzReleaseMediaTrackRecordingModel
                {
                    Id = record.ReleaseTrackRecordingId,
                    Title = record.ReleaseTrackTitle,
                    Length = record.ReleaseTrackLength,
                    Video = record.ReleaseTrackRecordingVideo
                }
            })
            .ToList();
        
        
        
        return null;
    }
}