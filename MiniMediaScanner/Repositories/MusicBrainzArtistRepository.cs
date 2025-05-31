using System.Runtime.InteropServices.JavaScript;
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
    
    public async Task<List<string>> GetArtistIdsByNameAsync(string artist)
    {
        string query = @"SELECT cast(ArtistId as text) FROM MusicBrainz_Artist where lower(name) = lower(@artist)";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn
                .QueryAsync<string>(query, new
                {
                    artist
                }))
            .ToList();
    }
    
    public async Task<string?> GetArtistCountryByNameAsync(string artist)
    {
        string query = @"SELECT country FROM MusicBrainz_Artist where lower(name) = lower(@artist)";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn
            .ExecuteScalarAsync<string>(query, new
            {
                artist
            });
    }
    
    public async Task<List<string>> GetAllArtistIdsAsync()
    {
        string query = @"SELECT cast(ArtistId as text) FROM MusicBrainz_Artist order by name asc";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        return (await conn
            .QueryAsync<string>(query))
            .ToList();
    }

    public async Task<Guid> UpsertArtistAsync(
        Guid artistId, 
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
        
        string query = @"INSERT INTO MusicBrainz_Artist (ArtistId, 
                               Name, Type, Country, SortName, Disambiguation,  LastSyncTime)
                         VALUES (@artistId, @name, @type, @Country, @SortName, @Disambiguation, @lastSyncTime)
                         ON CONFLICT (ArtistId) 
                         DO UPDATE SET 
                             Name = EXCLUDED.Name, 
                             Type = EXCLUDED.Type, 
                             Country = EXCLUDED.Country, 
                             SortName = EXCLUDED.SortName, 
                             Disambiguation = EXCLUDED.Disambiguation
                         RETURNING ArtistId";
        
        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.ExecuteScalarAsync<Guid>(query, new
            {
                artistId,
                name = artistName,
                type = artistType,
                Country = country,
                SortName = sortName,
                Disambiguation = disambiguation,
                lastSyncTime = new DateTime(2000, 1, 1)
            });
    }
    
    public async Task<DateTime> GetArtistLastSyncTimeAsync(Guid artistId)
    {
        string query = @"SELECT lastsynctime FROM MusicBrainz_Artist WHERE ArtistId = @artistId";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.ExecuteScalarAsync<DateTime>(query, new
        {
            artistId
        });
    }
    
    public async Task<DateTime> SetArtistLastSyncTimeAsync(Guid artistId)
    {
        string query = @"UPDATE MusicBrainz_Artist SET lastsynctime = @lastsynctime WHERE ArtistId = @artistId";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.ExecuteScalarAsync<DateTime>(query, new
        {
            artistId,
            lastsynctime = DateTime.Now
        });
    }

    public async Task<List<MusicBrainzSplitArtistModel>> GetSplitArtistAsync(string artistName)
    {
        string query = @"SELECT 
                             a.artistid,
                             a.name, 
                             coalesce(a.country, re.country) as Country, 
                             a.type, 
                             re.date,
                             re.country
                         FROM MusicBrainz_Artist a
                         LEFT JOIN LATERAL (
                             SELECT re.date AS date, re.country
                             FROM MusicBrainz_Release re
                             WHERE re.artistid = a.artistid
                             ORDER BY re.date ASC
                             LIMIT 1
                         ) re ON true
                         where lower(a.name) = lower(@artistName)
                         GROUP BY a.artistid, a.name, coalesce(a.country, re.country), a.type, re.date, re.country";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn.QueryAsync<MusicBrainzSplitArtistModel>(query, new
        {
            artistName
        })).ToList();
    }

    public async Task<Guid?> GetRecordingIdByNameAsync(string artistName, string albumName, string trackName)
    {
        const string query = @"SELECT rt.recordingid
                               FROM MusicBrainz_Release_Track rt
                               join MusicBrainz_Release r on r.releaseid = rt.releaseid
                               join MusicBrainz_Artist a on a.artistid = r.artistid 
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
    public async Task<List<MusicBrainzArtistModel>> GetMusicBrainzDataByNameAsync(string artistName, string albumName, string trackName)
    {
        const string query = @"
                                SELECT 
                                    a.ArtistId AS ArtistId,
                                    a.releasecount AS ArtistReleaseCount,
                                    a.disambiguation AS ArtistDisambiguation,
                                    a.name AS ArtistName,
                                    a.sortname AS ArtistSortName,
                                    a.type AS ArtistType,
                                    a.country AS ArtistCountry,
                                    
                                    r.ReleaseId AS ReleaseId,
                                    r.title AS ReleaseTitle,
                                    r.status AS ReleaseStatus,
                                    r.statusid AS ReleaseStatusId,
                                    r.date AS ReleaseDate,
                                    r.barcode AS ReleaseBarcode,
                                    r.country AS ReleaseCountry,
                                    r.disambiguation AS ReleaseDisambiguation,
                                    r.quality AS ReleaseQuality,
                                    rt.recordingid AS ReleaseTrackRecordingId,
                                    rt.ReleaseTrackId AS ReleaseTrackId,
                                    rt.mediatrackcount AS ReleaseTrackMediaTrackCount,
                                    rt.mediaformat AS ReleaseTrackMediaFormat,
                                    rt.title AS ReleaseTrackTitle,
                                    rt.recordingtitle AS ReleaseTrackRecordingTitle,
                                    rt.position AS ReleaseTrackPosition,
                                    rt.mediatrackoffset AS ReleaseTrackMediaTrackOffset,
                                    rt.length AS ReleaseTrackLength,
                                    rt.number AS ReleaseTrackNumber,
                                    rt.recordingvideo AS ReleaseTrackRecordingVideo,
                                    rt.mediaposition as ReleaseTrackDiscNumber,
                                    rl.CataLogNumber AS ReleaseCatalogNumber,
                                    
                                    rtaa.ArtistId AS TrackArtistId,
                                    rtaa.releasecount AS TrackArtistReleaseCount,
                                    rtaa.disambiguation AS TrackArtistDisambiguation,
                                    rtaa.name AS TrackArtistName,
                                    rtaa.sortname AS TrackArtistSortName,
                                    rtaa.type AS TrackArtistType,
                                    rtaa.country AS TrackArtistCountry,
                                    rta.JoinPhrase AS TrackArtistJoinPhrase,
                                    rta.Index AS TrackArtistIndex,
                                    
                                    label.LabelId,
                                    label.name AS LabelName,
                                    label.LabelCode,
                                    label.country AS LabelCountry
                                FROM MusicBrainz_Release_Track rt
                                join MusicBrainz_Release r on r.releaseid = rt.releaseid
                                join MusicBrainz_Artist a on a.artistid = r.artistid 
                                left join MusicBrainz_Release_Track_Artist rta on rta.releasetrackid = rt.releasetrackid
                                left join MusicBrainz_Artist rtaa on rtaa.artistid = rta.artistid
                                left join MusicBrainz_Release_Label rl on rl.releaseid = r.releaseid
                                left join MusicBrainz_Label label on label.labelid = rl.labelid
                                WHERE lower(a.name) = lower(@artistName)
                                      AND lower(r.status) = 'official'
                                      AND (length(@albumName) = 0 OR similarity(lower(r.title), lower(@albumName)) >= 0.80)
                                      AND (length(@trackName) = 0 OR similarity(lower(rt.title), lower(@trackName)) >= 0.80)";

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
            return new List<MusicBrainzArtistModel>();
        }

        List<MusicBrainzArtistModel> artistModels = new List<MusicBrainzArtistModel>();

        foreach (var artistGroup in records.GroupBy(r => r.ArtistId))
        {
            MusicBrainzArtistModel artistModel = new MusicBrainzArtistModel();
            
            artistModel.ArtistCredit = artistGroup
                .DistinctBy(record => record.ArtistId)
                .Select(record => new MusicBrainzArtistCreditModel
                {
                    Name = record.ArtistName,
                    Artist = new MusicBrainzArtistCreditEntityModel
                    {
                        Disambiguation = record.ArtistDisambiguation,
                        Name = record.ArtistName,
                        Type = record.ArtistType,
                        Id = record.ArtistId.ToString(),
                        SortName = record.ArtistSortName,
                        Country = record.ArtistCountry
                    }
                })
                .ToList();
            
            foreach (var releaseGroup in artistGroup.GroupBy(r => r.ReleaseId))
            {
                var releaseModel = releaseGroup
                    .DistinctBy(record => record.ReleaseId)
                    .Select(record => new MusicBrainzArtistReleaseModel
                    {
                        Id = record.ReleaseId.ToString(),
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
                        },
                        LabeLInfo = releaseGroup
                            .Where(record => record.LabelId.HasValue)
                            .DistinctBy(record => record.LabelId)
                            .Select(label =>
                                new MusicBrainzLabelInfoModel
                                {
                                    CataLogNumber = label.ReleaseCatalogNumber,
                                    Label = new MusicBrainzLabelInfoLabelModel
                                    {
                                        Country = label.LabelCountry,
                                        Id = label.LabelId.ToString(),
                                        Name = label.LabelName,
                                        LabelCode = label.LabelCode
                                    }
                                }).ToList()
                    })
                    .First();
                
                var releaseTracks = releaseGroup
                    .DistinctBy(record => record.ReleaseTrackRecordingId)
                    .Select(record => new MusicBrainzReleaseMediaTrackModel
                    {
                        Id = record.ReleaseTrackId.ToString(),
                        Title = record.ReleaseTrackTitle,
                        Position = record.ReleaseTrackPosition,
                        Length = record.ReleaseTrackLength,
                        Number = record.ReleaseTrackNumber,
                        Recording = new MusicBrainzReleaseMediaTrackRecordingModel
                        {
                            Id = record.ReleaseTrackRecordingId.ToString(),
                            Title = record.ReleaseTrackRecordingTitle,
                            Length = record.ReleaseTrackLength,
                            Video = record.ReleaseTrackRecordingVideo,
                            ArtistCredit = releaseGroup
                                .Where(artistRecord => artistRecord.ReleaseTrackRecordingId == record.ReleaseTrackRecordingId)
                                .DistinctBy(artistRecord => artistRecord.TrackArtistId)
                                .OrderBy(artistRecord => artistRecord.TrackArtistIndex)
                                .Select(artistRecord => new MusicBrainzArtistCreditModel
                                {
                                    Name = artistRecord.TrackArtistName,
                                    JoinPhrase = artistRecord.TrackArtistJoinPhrase,
                                    Artist = new MusicBrainzArtistCreditEntityModel
                                    {
                                        Id = artistRecord.TrackArtistId.ToString(),
                                        Name = artistRecord.TrackArtistName,
                                        Country = artistRecord.TrackArtistCountry,
                                        Disambiguation = artistRecord.TrackArtistDisambiguation,
                                        SortName = artistRecord.TrackArtistSortName,
                                        Type = artistRecord.TrackArtistType
                                    }
                                }).ToList()
                        }
                    })
                    .ToList();
                
                releaseModel.Media.Add(new MusicBrainzReleaseMediaModel
                {
                    Position = releaseGroup.First().ReleaseTrackDiscNumber,
                    Title = releaseGroup.First().ReleaseTitle,
                    Format = releaseGroup.First().ReleaseTrackMediaFormat,
                    TrackCount = releaseGroup.First().ArtistReleaseCount,
                    TrackOffset = releaseGroup.First().ReleaseTrackMediaTrackOffset,
                    Tracks = releaseTracks
                });
                
                artistModel.Releases.Add(releaseModel);
            }
            artistModels.Add(artistModel);
        }
        
        
        return artistModels;
    }
    
    public async Task<bool> ArtistExistsByIdAsync(Guid artistId)
    {
        string query = @"SELECT 1
                         FROM MusicBrainz_Artist artist
                         where artist.ArtistId = @artistId
                         limit 1";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn
            .ExecuteScalarAsync<int?>(query,
                param: new
                {
                    artistId
                })) == 1;
    }
}