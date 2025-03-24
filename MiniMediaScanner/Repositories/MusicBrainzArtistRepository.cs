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
    
    public async Task<List<string>> GetMusicBrainzArtistRemoteIdsByNameAsync(string artist)
    {
        string query = @"SELECT cast(MusicBrainzRemoteId as text) FROM MusicBrainzArtist where lower(name) = lower(@artist)";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn
                .QueryAsync<string>(query, new
                {
                    artist
                }))
            .ToList();
    }
    
    public async Task<string?> GetMusicBrainzArtistCountryByNameAsync(string artist)
    {
        string query = @"SELECT country FROM MusicBrainzArtist where lower(name) = lower(@artist)";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn
            .ExecuteScalarAsync<string>(query, new
            {
                artist
            });
    }
    
    public async Task<List<string>> GetAllMusicBrainzArtistRemoteIdsAsync()
    {
        string query = @"SELECT cast(MusicBrainzRemoteId as text) FROM MusicBrainzArtist order by name asc";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        return (await conn
            .QueryAsync<string>(query))
            .ToList();
    }
    
    public async Task<List<Guid>> GetMusicBrainzArtistIdsByNameAsync(string artistName)
    {
        string query = @"SELECT MusicBrainzArtistId FROM MusicBrainzArtist where lower(name) = lower(@artistName)";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn
            .QueryAsync<Guid>(query, new
            {
                artistName
            })).ToList();
    }
    
    public async Task<List<Guid>> GetAllMusicBrainzArtistIdsAsync()
    {
        string query = @"SELECT MusicBrainzArtistId FROM MusicBrainzArtist";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn
            .QueryAsync<Guid>(query))
            .ToList();
    }

    public async Task<Guid> InsertMusicBrainzArtistAsync(Guid remoteMusicBrainzArtistId, 
        string artistName, 
        string? artistType,
        string? country,
        string? sortName,
        string? disambiguation)
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
        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.ExecuteScalarAsync<Guid>(query, new
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
    
    public async Task<Guid?> GetRemoteMusicBrainzArtistIdAsync(string remoteMusicBrainzArtistId)
    {
        string query = @"SELECT MusicBrainzArtistId FROM MusicBrainzArtist WHERE MusicBrainzRemoteId = @id";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.ExecuteScalarAsync<Guid?>(query, new
            {
                id = remoteMusicBrainzArtistId
            });
    }
    public async Task<DateTime> GetBrainzArtistLastSyncTimeAsync(Guid remoteMusicBrainzArtistId)
    {
        string query = @"SELECT lastsynctime FROM MusicBrainzArtist WHERE MusicBrainzRemoteId = @id";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.ExecuteScalarAsync<DateTime>(query, new
        {
            id = remoteMusicBrainzArtistId
        });
    }

    public async Task<List<MusicBrainzSplitArtistModel>> GetSplitBrainzArtistAsync(string artistName)
    {
        string query = @"SELECT 
                             a.musicbrainzremoteid,
                             a.name, 
                             coalesce(a.country, re.country) as Country, 
                             a.type, 
                             re.date,
                             re.country
                         FROM musicbrainzartist a
                         LEFT JOIN LATERAL (
                             SELECT re.date AS date, re.country
                             FROM musicbrainzrelease re
                             WHERE re.musicbrainzartistid = a.musicbrainzartistid
                             ORDER BY re.date ASC
                             LIMIT 1
                         ) re ON true
                         where lower(a.name) = lower(@artistName)
                         GROUP BY a.musicbrainzremoteid, a.name, coalesce(a.country, re.country), a.type, re.date, re.country";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn.QueryAsync<MusicBrainzSplitArtistModel>(query, new
        {
            artistName
        })).ToList();
    }

    public async Task<Guid?> GetMusicBrainzRecordingIdByNameAsync(string artistName, string albumName, string trackName)
    {
        const string query = @"SELECT rt.recordingid
                               FROM musicbrainzreleasetrack rt
                               join musicbrainzrelease r on r.musicbrainzremotereleaseid = rt.musicbrainzremotereleaseid
                               join musicbrainzartist a on a.musicbrainzartistid = r.musicbrainzartistid 
                               WHERE lower(a.name) = lower(@artistName)
                                     AND lower(r.title) = lower(@albumName)
                                     AND lower(rt.title) = lower(@trackName)
                               limit 1";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<Guid>(query, 
            param: new
            {
                artistName,
                albumName,
                trackName
            });
    }
    public async Task<MusicBrainzArtistModel?> GetMusicBrainzDataByNameAsync(string artistName, string albumName, string trackName)
    {
        const string query = @"
                                SELECT 
                                    a.musicbrainzremoteid AS ArtistMusicBrainzRemoteId,
                                    a.releasecount AS ArtistReleaseCount,
                                    a.disambiguation AS ArtistDisambiguation,
                                    a.name AS ArtistName,
                                    a.sortname AS ArtistSortName,
                                    a.type AS ArtistType,
                                    a.country AS ArtistCountry,
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
                                    rt.recordingvideo AS ReleaseTrackRecordingVideo,
                                    rt.mediaposition as ReleaseTrackDiscNumber
                                FROM musicbrainzreleasetrack rt
                                join musicbrainzrelease r on r.musicbrainzremotereleaseid = rt.musicbrainzremotereleaseid
                                join musicbrainzartist a on a.musicbrainzartistid = r.musicbrainzartistid 
                                WHERE lower(a.name) = lower(@artistName)
                                      AND lower(r.status) = 'official'
                                      AND (length(@albumName) = 0 OR lower(r.title) = lower(@albumName))
                                      AND (length(@trackName) = 0 OR lower(rt.title) = lower(@trackName))";

        await using var conn = new NpgsqlConnection(_connectionString);
        var records = await conn.QueryAsync<MusicBrainzRecordingFlatModel>(query, 
            param: new
            {
                artistName,
                albumName,
                trackName
            });

        if (records.Count() == 0)
        {
            return null;
        }

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
                    Id = record.ArtistMusicBrainzRemoteId.ToString(),
                    SortName = record.ArtistSortName,
                    Country = record.ArtistCountry
                }
                
            })
            .ToList();
        
        artistModel.Releases = records
            .GroupBy(record => record.ReleaseMusicBrainzRemoteReleaseId)
            .Select(record => record.First())
            .Select(record => new MusicBrainzArtistReleaseModel
            {
                Id = record.ReleaseMusicBrainzRemoteReleaseId.ToString(),
                Title = record.ReleaseTitle,
                Status = record.ReleaseStatus,
                StatusId = record.ReleaseStatusId,
                Date = record.ReleaseDate,
                Barcode = record.ReleaseBarcode,
                Country = record.ReleaseCountry,
                Disambiguation = record.ReleaseDisambiguation,
                Quality = record.ReleaseQuality,
                ReleaseGroup = new MusicBrainzReleaseGroupModel(),
                TextRepresentation = new MusicBrainzTextRepresentationModel
                {
                    Language = string.Empty,
                    Script = string.Empty
                }
            })
            .ToList();

        foreach (var releaseGroup in records.GroupBy(r => r.ReleaseMusicBrainzRemoteReleaseId))
        {
            var releaseTracks = records
                .Where(record => record.ReleaseMusicBrainzRemoteReleaseId == releaseGroup.Key)
                .GroupBy(record => record.ReleaseTrackRecordingId)
                .Select(record => record.First())
                .Select(record => new MusicBrainzReleaseMediaTrackModel
                {
                    Id = record.ReleaseTrackMusicBrainzRemoteReleaseTrackId.ToString(),
                    Title = record.ReleaseTrackTitle,
                    Position = record.ReleaseTrackPosition,
                    Length = record.ReleaseTrackLength,
                    Number = record.ReleaseTrackNumber,
                    Recording = new MusicBrainzReleaseMediaTrackRecordingModel
                    {
                        Id = record.ReleaseTrackRecordingId.ToString(),
                        Title = record.ReleaseTrackTitle,
                        Length = record.ReleaseTrackLength,
                        Video = record.ReleaseTrackRecordingVideo
                    }
                })
                .ToList();

            var artistRelease = artistModel.Releases.FirstOrDefault(r => r.Id == releaseGroup.Key.ToString());
            if (artistRelease != null)
            {
                artistRelease.Media.Add(new MusicBrainzReleaseMediaModel
                {
                    Position = records.First().ReleaseTrackDiscNumber,
                    Title = records.First().ReleaseTitle,
                    Format = records.First().ReleaseTrackMediaFormat,
                    TrackCount = records.First().ArtistReleaseCount,
                    TrackOffset = records.First().ReleaseTrackMediaTrackOffset,
                    Tracks = releaseTracks
                });
            }
        }
        
        return artistModel;
    }
}