using System.Text;
using MiniMediaScanner.Models;
using Npgsql;
using Dapper;

namespace MiniMediaScanner.Repositories;

public class MetadataRepository
{
    private readonly string _connectionString;
    public MetadataRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task UpdateMetadataFingerprintAsync(string metadataId, string fingerprint, float duration,
        DateTime File_LastWriteTime, DateTime File_CreationTime)
    {
        string query = @"UPDATE metadata SET 
                                tag_acoustidfingerprint = @fingerprint,
                                tag_acoustidfingerprint_duration = @duration,
                                file_lastwritetime = @File_LastWriteTime,
                                file_creationtime = @File_CreationTime
                         WHERE MetadataId = cast(@id as uuid)";

        await using var conn = new NpgsqlConnection(_connectionString);

        await conn.ExecuteAsync(query, new
        {
            id = metadataId,
            fingerprint,
            duration,
            File_LastWriteTime,
            File_CreationTime
        });
    }
    
    public async Task UpdateMetadataPathAsync(string metadataId, string path)
    {
        string query = @"UPDATE metadata SET Path = @path WHERE MetadataId = cast(@id as uuid)";

        await using var conn = new NpgsqlConnection(_connectionString);

        await conn.ExecuteAsync(query, new
        {
            id = metadataId,
            path
        });
    }
    
    
    
    public async Task<List<string>> GetMissingTracksByArtistAsync(List<string> artistNames)
    {
        StringBuilder filter = new StringBuilder();

        int index = 0;
        foreach (string artistName in artistNames)
        {
            if (index == 0)
            {
                filter.AppendLine($"m.tag_alljsontags @> jsonb_build_object('artist', '{artistName}', 'title', ut.track_title)");
            }
            else
            {
                filter.AppendLine($"or m.tag_alljsontags @> jsonb_build_object('artist', '{artistName}', 'title', ut.track_title)");
            }

            filter.AppendLine($"or m.tag_alljsontags @> jsonb_build_object('AlbumArtist', '{artistName}', 'title', ut.track_title)");
            filter.AppendLine($"or (lower(m.tag_alljsontags->>'ARTISTS') ilike '%{artistName}%' and lower(m.tag_alljsontags->>'title') = lower(ut.track_title))");
            index++;
        }
        
        string query = @$"WITH unique_tracks AS (
                         SELECT *
                         FROM (
                             select lower(re.title) as album_title, lower(ar.name) as artist_name, track.title as track_title, lower(re.status) as status,
                                    ROW_NUMBER() OVER (
                                        PARTITION BY track.title, lower(re.title), lower(ar.name)
                                    ) AS rn
                                    
                               FROM MusicBrainz_Artist ar
                                 JOIN MusicBrainz_Release re 
                                     ON re.musicbrainzartistid = ar.musicbrainzartistid
                                     --AND lower(re.country) = lower(ar.country)
                                      AND (lower(re.status) = 'official' OR LENGTH(re.status) = 0)
                                 JOIN MusicBrainz_Release_Track track 
                                     ON track.musicbrainzremotereleaseid = re.musicbrainzremotereleaseid
                         ) AS subquery
                             WHERE rn = 1
                     )
                     SELECT distinct ut.artist_name || ' - ' || ut.album_title || ' - ' || ut.track_title
                     FROM unique_tracks ut
 
                     left join metadata m on {filter}
 
                     where ut.artist_name = lower(@artistName)
                     and m.metadataid is null";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        return conn
            .Query<string>(query, param: new {artistName = artistNames.First()}, commandTimeout: 9999)
            .ToList();
    }
    
    public async Task<List<MetadataModel>> GetDuplicateFileVersionsAsync(string artistName)
    {
        string query = @"select m.MetadataId, m.Path, m.Title, album.albumId
                         from artists artist
                         join albums album on album.artistid = artist.artistid
                         join metadata m on m.albumid = album.albumid
                         where 
                         artist.""name"" = @artistName
                         and m.""path"" ~ '\([0-9]*\)\.([a-zA-Z0-9]{2,5})'";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        return conn.Query<MetadataModel>(query, 
                new
                {
                    artistName
                })
            .ToList();
    }
    public async Task<List<DuplicateFileExtensionModel>> GetDuplicateFileExtensionsAsync(string artistName)
    {
        string query = @"WITH duplicates AS (
                              SELECT 
                                  m.MetadataId,
                                  m.Path,
                                  m.Title,
                                  album.AlbumId,
                                  REGEXP_REPLACE(m.Path, '\.([a-zA-Z0-9]{2,5})$', '') AS FilePathWithoutExtension,
                                  COUNT(*) OVER (PARTITION BY album.albumId, REGEXP_REPLACE(m.Path, '\.([a-zA-Z0-9]{2,5})$', '')) AS duplicate_count
                              FROM artists artist
                              JOIN albums album ON album.artistid = artist.artistid
                              JOIN metadata m ON m.albumid = album.albumid
                              WHERE LOWER(artist.name) = lower(@artistName)
                          )
                          SELECT MetadataId, Path, Title, AlbumId, FilePathWithoutExtension, duplicate_count
                          FROM duplicates
                          WHERE duplicate_count > 1";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        return conn.Query<DuplicateFileExtensionModel>(query, 
                new
                {
                    artistName
                })
            .ToList();
    }
    public async Task<List<DuplicateAlbumFileNameModel>> GetDuplicateAlbumFileNamesAsync(string artistName, int accuracy)
    {
        if (accuracy >= 100)
        {
            accuracy = 99;
        }
        string query = @$"SET LOCAL pg_trgm.similarity_threshold = 0.{accuracy};
                          WITH filenames AS (
                              SELECT 
                                  m.MetadataId,
                                  m.Path,
                                  m.Title,
                                  album.AlbumId,
                                  REGEXP_REPLACE(m.Path, '^.*/([^/]*/[^/]+)$', '\1', 'g') AS FileName
                              FROM artists artist
                              JOIN albums album ON album.artistid = artist.artistid
                              JOIN metadata m ON m.albumid = album.albumid
                              WHERE LOWER(artist.name) = LOWER(@artistName)
                          ),
                          similar_groups AS (
                              SELECT 
                                  f1.MetadataId,
                                  f1.Path,
                                  f1.Title,
                                  f1.AlbumId,
                                  f1.FileName,
                                  COUNT(*) AS duplicate_count
                              FROM filenames f1
                              JOIN filenames f2 
                                  ON f1.AlbumId = f2.AlbumId
                                 AND f1.MetadataId != f2.MetadataId
                                 AND f1.FileName % f2.FileName
                              GROUP BY f1.MetadataId, f1.Path, f1.Title, f1.AlbumId, f1.FileName
                          )
                          SELECT *
                          FROM similar_groups
                          WHERE duplicate_count > 0
                          ORDER BY AlbumId, FileName, Path";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        var transaction = await conn.BeginTransactionAsync();
        var filenames = new List<DuplicateAlbumFileNameModel>();
        
        try
        {
            filenames = conn.Query<DuplicateAlbumFileNameModel>(query, 
                    new
                    {
                        artistName
                    }, commandTimeout: 120,
                    transaction: transaction)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
        }
        finally
        {
            await transaction.CommitAsync();
        }

        return filenames;
    }
    
    
    public async Task<List<DuplicateAlbumFileNameModel>> GetDuplicateAlbumFileExtensionsAsync(string artistName)
    {
        string query = @"WITH duplicates AS (
                             SELECT 
                                 m.MetadataId,
                                 m.Path,
                                 REGEXP_REPLACE(m.Path, '\.([a-zA-Z0-9]{2,5})$', '') AS PathWithoutExtension,
                                 m.Title,
                                 album.AlbumId,
                                 REGEXP_REPLACE(
          	                       REGEXP_REPLACE(m.Path, '^.*/([^/]*/[^/]+)$', '\1', 'g'), 
          			   				                     '\.([a-zA-Z0-9]{2,5})$', '') AS FileName,
                                 COUNT(*) OVER (PARTITION BY album.albumId, 
          	                       lower(REGEXP_REPLACE(REGEXP_REPLACE(m.Path, '^.*/([^/]*/[^/]+)$', '\1', 'g'), 
          			   								                    '\.([a-zA-Z0-9]{2,5})$', ''))) AS duplicate_count
                             FROM artists artist
                             JOIN albums album ON album.artistid = artist.artistid
                             JOIN metadata m ON m.albumid = album.albumid
                             WHERE LOWER(artist.name) = lower(@artistName)
                         )
                         SELECT MetadataId, PathWithoutExtension, Path, Title, AlbumId, FileName, duplicate_count
                         FROM duplicates
                         WHERE duplicate_count > 1
                         ORDER BY AlbumId, FileName, Path";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        var transaction = await conn.BeginTransactionAsync();
        var filenames = new List<DuplicateAlbumFileNameModel>();
        
        try
        {
            filenames = conn.Query<DuplicateAlbumFileNameModel>(query, 
                    new
                    {
                        artistName
                    }, commandTimeout: 120,
                    transaction: transaction)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
        }
        finally
        {
            await transaction.CommitAsync();
        }

        return filenames;
    }
    
    public async Task<List<MetadataInfo>> GetMissingMusicBrainzMetadataRecordsAsync(string artistName)
    {
        string query = @$"SELECT m.MetadataId, 
                                  m.Path, 
                                  m.Title, 
                                  m.AlbumId, 
                                  m.MusicBrainzArtistId, 
                                  m.MusicBrainzDiscId, 
                                  m.MusicBrainzReleaseCountry, 
                                  m.MusicBrainzReleaseId, 
                                  m.MusicBrainzTrackId, 
                                  m.MusicBrainzReleaseStatus, 
                                  m.MusicBrainzReleaseType,
                                  m.MusicBrainzReleaseArtistId,
                                  m.MusicBrainzReleaseGroupId,
                                  m.Tag_Subtitle, 
                                  m.Tag_AlbumSort, 
                                  m.Tag_Comment, 
                                  m.Tag_Year, 
                                  m.Tag_Track, 
                                  m.Tag_TrackCount, 
                                  m.Tag_Disc, 
                                  m.Tag_DiscCount, 
                                  m.Tag_Lyrics, 
                                  m.Tag_Grouping, 
                                  m.Tag_BeatsPerMinute, 
                                  m.Tag_Conductor, 
                                  m.Tag_Copyright, 
                                  m.Tag_DateTagged, 
                                  m.Tag_AmazonId,
                                  m.Tag_ReplayGainTrackGain, 
                                  m.Tag_ReplayGainTrackPeak, 
                                  m.Tag_ReplayGainAlbumGain, 
                                  m.Tag_ReplayGainAlbumPeak, 
                                  m.Tag_InitialKey, 
                                  m.Tag_RemixedBy, 
                                  m.Tag_Publisher, 
                                  m.Tag_ISRC, 
                                  m.Tag_Length, 
                                  m.Tag_AcoustIdFingerPrint, 
                                  m.Tag_AcoustId,
                                  m.Tag_AcoustIdFingerPrint_Duration,
                                  album.title AS Album,
                                  artist.artistid AS ArtistId
                        FROM metadata m
                        JOIN albums album ON album.albumid = m.albumid
                        JOIN artists artist ON artist.artistid = album.artistid
                        where lower(artist.name) = lower(@artistName)
                        and (length(m.MusicBrainzArtistId) = 0 or 
                              length(m.MusicBrainzTrackId) = 0 or
                              length(m.MusicBrainzReleaseArtistId) = 0 or
                              length(m.MusicBrainzReleaseGroupId) = 0 or
                              tag_alljsontags->>'ARTISTS' is null)
                              and length(m.tag_acoustidfingerprint) > 0
                              and m.Tag_AcoustIdFingerPrint_Duration > 0";

        await using var conn = new NpgsqlConnection(_connectionString);

        return conn
            .Query<MetadataInfo>(query, new
            {
                artistName
            }).ToList();
    }
    
    public async Task<List<MetadataInfo>> GetMissingSpotifyMetadataRecordsAsync(string artistName)
    {
        string query = @$"SELECT m.MetadataId, 
                                  m.Path, 
                                  m.Title, 
                                  m.AlbumId, 
                                  m.MusicBrainzArtistId, 
                                  m.MusicBrainzDiscId, 
                                  m.MusicBrainzReleaseCountry, 
                                  m.MusicBrainzReleaseId, 
                                  m.MusicBrainzTrackId, 
                                  m.MusicBrainzReleaseStatus, 
                                  m.MusicBrainzReleaseType,
                                  m.MusicBrainzReleaseArtistId,
                                  m.MusicBrainzReleaseGroupId,
                                  m.Tag_Subtitle, 
                                  m.Tag_AlbumSort, 
                                  m.Tag_Comment, 
                                  m.Tag_Year, 
                                  m.Tag_Track, 
                                  m.Tag_TrackCount, 
                                  m.Tag_Disc, 
                                  m.Tag_DiscCount, 
                                  m.Tag_Lyrics, 
                                  m.Tag_Grouping, 
                                  m.Tag_BeatsPerMinute, 
                                  m.Tag_Conductor, 
                                  m.Tag_Copyright, 
                                  m.Tag_DateTagged, 
                                  m.Tag_AmazonId,
                                  m.Tag_ReplayGainTrackGain, 
                                  m.Tag_ReplayGainTrackPeak, 
                                  m.Tag_ReplayGainAlbumGain, 
                                  m.Tag_ReplayGainAlbumPeak, 
                                  m.Tag_InitialKey, 
                                  m.Tag_RemixedBy, 
                                  m.Tag_Publisher, 
                                  m.Tag_ISRC, 
                                  m.Tag_Length, 
                                  m.Tag_AcoustIdFingerPrint, 
                                  m.Tag_AcoustId,
                                  m.Tag_AcoustIdFingerPrint_Duration,
                                  album.title AS Album,
                                  artist.name AS Artist,
                                  artist.artistid AS ArtistId
                        FROM metadata m
                        JOIN albums album ON album.albumid = m.albumid
                        JOIN artists artist ON artist.artistid = album.artistid
                        where lower(artist.name) = lower(@artistName)
                        and (tag_alljsontags->>'Spotify Track Id' is null or 
                             tag_alljsontags->>'Spotify Album Id' is null or
                             tag_alljsontags->>'Spotify Track Type Id' is null or
                              tag_alljsontags->>'ARTISTS' is null)";

        await using var conn = new NpgsqlConnection(_connectionString);

        return conn
            .Query<MetadataInfo>(query, new
            {
                artistName
            }).ToList();
    }
    
    public async Task<List<MetadataInfo>> GetMissingTidalMetadataRecordsAsync(string artistName)
    {
        string query = @$"SELECT m.MetadataId, 
                                  m.Path, 
                                  m.Title, 
                                  m.AlbumId, 
                                  m.MusicBrainzArtistId, 
                                  m.MusicBrainzDiscId, 
                                  m.MusicBrainzReleaseCountry, 
                                  m.MusicBrainzReleaseId, 
                                  m.MusicBrainzTrackId, 
                                  m.MusicBrainzReleaseStatus, 
                                  m.MusicBrainzReleaseType,
                                  m.MusicBrainzReleaseArtistId,
                                  m.MusicBrainzReleaseGroupId,
                                  m.Tag_Subtitle, 
                                  m.Tag_AlbumSort, 
                                  m.Tag_Comment, 
                                  m.Tag_Year, 
                                  m.Tag_Track, 
                                  m.Tag_TrackCount, 
                                  m.Tag_Disc, 
                                  m.Tag_DiscCount, 
                                  m.Tag_Lyrics, 
                                  m.Tag_Grouping, 
                                  m.Tag_BeatsPerMinute, 
                                  m.Tag_Conductor, 
                                  m.Tag_Copyright, 
                                  m.Tag_DateTagged, 
                                  m.Tag_AmazonId,
                                  m.Tag_ReplayGainTrackGain, 
                                  m.Tag_ReplayGainTrackPeak, 
                                  m.Tag_ReplayGainAlbumGain, 
                                  m.Tag_ReplayGainAlbumPeak, 
                                  m.Tag_InitialKey, 
                                  m.Tag_RemixedBy, 
                                  m.Tag_Publisher, 
                                  m.Tag_ISRC, 
                                  m.Tag_Length, 
                                  m.Tag_AcoustIdFingerPrint, 
                                  m.Tag_AcoustId,
                                  m.Tag_AcoustIdFingerPrint_Duration,
                                  album.title AS Album,
                                  artist.name AS Artist,
                                  artist.artistid AS ArtistId
                        FROM metadata m
                        JOIN albums album ON album.albumid = m.albumid
                        JOIN artists artist ON artist.artistid = album.artistid
                        where lower(artist.name) = lower(@artistName)
                        and (tag_alljsontags->>'Tidal Track Id' is null or 
                             tag_alljsontags->>'Tidal Album Id' is null)";

        await using var conn = new NpgsqlConnection(_connectionString);

        return conn
            .Query<MetadataInfo>(query, new
            {
                artistName
            }).ToList();
    }
    
    
    public async Task<List<MetadataInfo>> GetMissingDeezerMetadataRecordsAsync(string artistName)
    {
        string query = @$"SELECT m.MetadataId, 
                                  m.Path, 
                                  m.Title, 
                                  m.AlbumId, 
                                  m.MusicBrainzArtistId, 
                                  m.MusicBrainzDiscId, 
                                  m.MusicBrainzReleaseCountry, 
                                  m.MusicBrainzReleaseId, 
                                  m.MusicBrainzTrackId, 
                                  m.MusicBrainzReleaseStatus, 
                                  m.MusicBrainzReleaseType,
                                  m.MusicBrainzReleaseArtistId,
                                  m.MusicBrainzReleaseGroupId,
                                  m.Tag_Subtitle, 
                                  m.Tag_AlbumSort, 
                                  m.Tag_Comment, 
                                  m.Tag_Year, 
                                  m.Tag_Track, 
                                  m.Tag_TrackCount, 
                                  m.Tag_Disc, 
                                  m.Tag_DiscCount, 
                                  m.Tag_Lyrics, 
                                  m.Tag_Grouping, 
                                  m.Tag_BeatsPerMinute, 
                                  m.Tag_Conductor, 
                                  m.Tag_Copyright, 
                                  m.Tag_DateTagged, 
                                  m.Tag_AmazonId,
                                  m.Tag_ReplayGainTrackGain, 
                                  m.Tag_ReplayGainTrackPeak, 
                                  m.Tag_ReplayGainAlbumGain, 
                                  m.Tag_ReplayGainAlbumPeak, 
                                  m.Tag_InitialKey, 
                                  m.Tag_RemixedBy, 
                                  m.Tag_Publisher, 
                                  m.Tag_ISRC, 
                                  m.Tag_Length, 
                                  m.Tag_AcoustIdFingerPrint, 
                                  m.Tag_AcoustId,
                                  m.Tag_AcoustIdFingerPrint_Duration,
                                  album.title AS Album,
                                  artist.name AS Artist,
                                  artist.artistid AS ArtistId
                        FROM metadata m
                        JOIN albums album ON album.albumid = m.albumid
                        JOIN artists artist ON artist.artistid = album.artistid
                        where lower(artist.name) = lower(@artistName)
                        and (tag_alljsontags->>'Deezer Track Id' is null or 
                             tag_alljsontags->>'Deezer Album Id' is null)";

        await using var conn = new NpgsqlConnection(_connectionString);

        return conn
            .Query<MetadataInfo>(query, new
            {
                artistName
            }).ToList();
    }
    
    public async Task<List<MetadataInfo>> GetMetadataByTagRecordsAsync(string artistName, List<String> tagNames)
    {
        string query = @$"SELECT m.MetadataId, 
                                  m.Path, 
                                  m.Title, 
                                  m.AlbumId, 
                                  m.MusicBrainzArtistId, 
                                  m.MusicBrainzDiscId, 
                                  m.MusicBrainzReleaseCountry, 
                                  m.MusicBrainzReleaseId, 
                                  m.MusicBrainzTrackId, 
                                  m.MusicBrainzReleaseStatus, 
                                  m.MusicBrainzReleaseType,
                                  m.MusicBrainzReleaseArtistId,
                                  m.MusicBrainzReleaseGroupId,
                                  m.Tag_Subtitle, 
                                  m.Tag_AlbumSort, 
                                  m.Tag_Comment, 
                                  m.Tag_Year, 
                                  m.Tag_Track, 
                                  m.Tag_TrackCount, 
                                  m.Tag_Disc, 
                                  m.Tag_DiscCount, 
                                  m.Tag_Lyrics, 
                                  m.Tag_Grouping, 
                                  m.Tag_BeatsPerMinute, 
                                  m.Tag_Conductor, 
                                  m.Tag_Copyright, 
                                  m.Tag_DateTagged, 
                                  m.Tag_AmazonId,
                                  m.Tag_ReplayGainTrackGain, 
                                  m.Tag_ReplayGainTrackPeak, 
                                  m.Tag_ReplayGainAlbumGain, 
                                  m.Tag_ReplayGainAlbumPeak, 
                                  m.Tag_InitialKey, 
                                  m.Tag_RemixedBy, 
                                  m.Tag_Publisher, 
                                  m.Tag_ISRC, 
                                  m.Tag_Length, 
                                  m.Tag_AcoustIdFingerPrint, 
                                  m.Tag_AcoustId,
                                  m.Tag_AcoustIdFingerPrint_Duration,
                                  album.title AS Album
                        FROM metadata m
                        JOIN albums album ON album.albumid = m.albumid
                        JOIN artists artist ON artist.artistid = album.artistid
                        where lower(artist.name) = lower(@artistName)
                              and m.tag_alljsontags ?| array[@tagNames]";

        await using var conn = new NpgsqlConnection(_connectionString);

        return conn
            .Query<MetadataInfo>(query, new
            {
                artistName,
                tagNames
            }, commandTimeout: 120).ToList();
    }
    
    
    public async Task<List<MetadataInfo>> GetMetadataByTagValueRecordsAsync(string artistName, string tagName, string value)
    {
        string query = @$"SELECT m.MetadataId, 
                                  m.Path, 
                                  m.Title, 
                                  m.AlbumId, 
                                  m.Tag_AllJsonTags,
                                  album.title AS Album
                        FROM metadata m
                        JOIN albums album ON album.albumid = m.albumid
                        JOIN artists artist ON artist.artistid = album.artistid
                        where lower(artist.name) = lower(@artistName)
                              and m.tag_alljsontags->>@tagName like '%' || @value ||'%'";

        await using var conn = new NpgsqlConnection(_connectionString);

        return conn
            .Query<MetadataInfo>(query, new
            {
                artistName,
                tagName,
                value
            }, commandTimeout: 120).ToList();
    }
    
    public async Task<List<MetadataModel>> GetAllMetadataPathsByMissingFingerprintAsync(string artistName)
    {
        string query = @$"SELECT m.MetadataId, m.Path
                        FROM metadata m
                        JOIN albums album ON album.albumid = m.albumid
                        JOIN artists artist ON artist.artistid = album.artistid
                        where lower(artist.name) = lower(@artistName)
                        and (length( m.tag_acoustidfingerprint) = 0
                            or m.tag_acoustidfingerprint_duration = 0)";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        return conn.Query<MetadataModel>(query, new
        {
            artistName
        }).ToList();
    }
    
    public async Task<List<MetadataModel>> GetMetadataByArtistAsync(string artistName)
    {
        string query = @$"SELECT m.MetadataId, 
                                 m.Path, 
                                 m.Title, 
                                 m.AlbumId,
                                 tag_alljsontags,
                                 album.title AS AlbumName,
                                 tag_track,
                                 tag_trackcount,
                                 tag_disc,
                                 tag_disccount,
                                 artist.name AS ArtistName,
                                 m.MusicBrainzArtistId,
                                 m.tag_acoustid,
                                 m.Tag_AllJsonTags,
                                 artist.ArtistId
                        FROM metadata m
                        JOIN albums album ON album.albumid = m.albumid
                        JOIN artists artist ON artist.artistid = album.artistid
                        where lower(artist.name) = lower(@artistName)";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        return conn.Query<MetadataModel>(query, new
        {
            artistName
        }).ToList();
    }
    
    public async Task<List<MetadataModel>> GetUntaggedMetadataByArtistAsync(string artistName, string[] providers)
    {
        string query = @$"SELECT m.MetadataId, 
                                 m.Path, 
                                 m.Title, 
                                 m.AlbumId,
                                 tag_alljsontags,
                                 album.title AS AlbumName,
                                 tag_track,
                                 tag_trackcount,
                                 tag_disc,
                                 tag_disccount,
                                 artist.name AS ArtistName,
                                 m.MusicBrainzArtistId,
                                 m.tag_acoustid,
                                 m.Tag_AllJsonTags,
                                 artist.ArtistId
                        FROM metadata m
                        JOIN albums album ON album.albumid = m.albumid
                        JOIN artists artist ON artist.artistid = album.artistid
                        left JOIN LATERAL (
	                         SELECT jsonb_object_keys(m.tag_alljsontags) AS key
	                     ) subquery ON lower(subquery.key) ilike all(@providers)
                        where lower(artist.name) = lower(@artistName)
                              and subquery.key is null ";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        return conn.Query<MetadataModel>(query, new
        {
            artistName,
            providers = providers.Select(provider => provider + "%").ToArray()
        }).ToList();
    }
    
    public async Task<List<MetadataModel>> GetMetadataByTagMissingArtistAsync(
        string artistFilter,
        string searchTag, 
        string labelName, 
        string artistName, 
        string albumRegex)
    {
        string query = @"SET LOCAL pg_trgm.similarity_threshold = 0.8;
                         select m.MetadataId, 
                              m.Path, 
                              m.Title, 
                              m.AlbumId,
                              tag_alljsontags,
                              album.title AS AlbumName,
                              tag_track,
                              tag_trackcount,
                              tag_disc,
                              tag_disccount,
                              artist.name AS ArtistName,
                              m.MusicBrainzArtistId,
                              m.tag_acoustid,
                              m.Tag_AllJsonTags,
                              artist.ArtistId
                         from metadata m
                         JOIN albums album ON album.albumid = m.albumid
                         JOIN artists artist ON artist.artistid = album.artistid
                         JOIN LATERAL (
	                         SELECT jsonb_object_keys(m.tag_alljsontags) AS key
                         ) artistskey ON LOWER(artistskey.key) LIKE 'artists'
                         JOIN LATERAL (
	                         SELECT jsonb_object_keys(m.tag_alljsontags) AS key
                         ) labelkey ON LOWER(labelkey.key) = @searchTag
                         where 
                             (@artistFilter is null or length(@artistFilter) = 0 or lower(artist.name) % lower(@artistFilter))
                             and regexp_like(album.Title,  @albumRegex)
                             and LOWER(m.tag_alljsontags->>labelkey.key) ILIKE '%' || @labelName || '%'
                             and not LOWER(m.tag_alljsontags->>artistskey.key) ILIKE '%' || @artistName || '%'";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        var transaction = await conn.BeginTransactionAsync();
        var metadata = new List<MetadataModel>();

        try
        {
            metadata = conn.Query<MetadataModel>(query, new
                               {
                                   artistFilter,
                                   searchTag,
                                   labelName,
                                   artistName,
                                   albumRegex
                               }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
        }
        finally
        {
            await transaction.CommitAsync();
        }

        return metadata;
    }
    
    public async Task<List<Guid?>> GetArtistIdByMetadataAsync(string artistName)
    {
        string query = @$"SELECT distinct artist.ArtistId
                        FROM metadata m
                        JOIN albums album ON album.albumid = m.albumid
                        JOIN artists artist ON artist.artistid = album.artistid
                        where lower(artist.name) = lower(@artistName)";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        return conn.Query<Guid?>(query, new
        {
            artistName
        }).ToList();
    }
    
    public async Task<List<MetadataPathCoverModel>> GetFolderPathsByArtistForCoversAsync(string artistName, string album)
    {
        string query = @$"SELECT distinct regexp_replace(path, '[^/]+$', '') AS FolderPath, 
                                 m.MusicBrainzReleaseId,
                                 artist.name as ArtistName,
                                 album.title AS AlbumName,
                                 artist.ArtistId AS ArtistId
                          FROM artists artist
                          JOIN albums album ON album.artistid = artist.artistid
                          JOIN metadata m ON m.albumid = album.albumid
                          where lower(artist.name) = lower(@artistName)
                                and (length(@album) = 0 or @album is null or lower(album.title) = lower(@album))";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        return conn.Query<MetadataPathCoverModel>(query, new
        {
            artistName,
            album
        }).ToList();
    }
    
    public async Task<List<MetadataModel>> GetMetadataByPathAsync(string targetPath)
    {
        string query = @$"SELECT m.MetadataId, 
                                 m.Path, 
                                 m.Title, 
                                 m.AlbumId,
                                 m.tag_acoustidfingerprint
                        FROM metadata m
                        where m.path = @path";

        await using var conn = new NpgsqlConnection(_connectionString);

        return conn
            .Query<MetadataModel>(query, new
            {
                path = targetPath
            }).ToList();
    }
    
    public async Task<List<string>> GetPathByLikePathAsync(string path)
    {
        string query = @$"SELECT m.Path
                        FROM metadata m
                        where m.path like @path || '%'";

        await using var conn = new NpgsqlConnection(_connectionString);

        return conn
            .Query<string>(query, new
            {
                path
            }).ToList();
    }
    
    public async Task<List<MetadataModel>> GetMetadataByFileExtensionAsync(string fileExtension)
    {
        string query = @$"SELECT m.MetadataId, 
                                 m.Path, 
                                 m.Title, 
                                 m.AlbumId
                        FROM metadata m
                        where m.path like '%.' || @fileExtension";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn
            .QueryAsync<MetadataModel>(query, new
            {
                fileExtension
            })).ToList();
    }
    
    public async Task DeleteMetadataRecordsAsync(List<string> metadataIds)
    {
        string query = @"DELETE FROM metadata WHERE metadataid = ANY(@id)";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        await conn.ExecuteAsync(query, new
        {
            id = metadataIds.Select(id => Guid.Parse(id)).ToList()
        });
    }
    
    public async Task<bool> MetadataCanUpdateAsync(string path, DateTime lastWriteTime, DateTime creationTime)
    {
        string query = @"SELECT MetadataId, File_LastWriteTime, File_CreationTime 
                         FROM metadata 
                         WHERE path = @path
                         LIMIT 1";
        
        await using var conn = new NpgsqlConnection(_connectionString);
        
        CanUpdateMetadataModel? canUpdateMetadataModel = await conn
            .QueryFirstOrDefaultAsync<CanUpdateMetadataModel>(query, new
            {
                path
            });

        bool canUpdate = true;
        if (canUpdateMetadataModel != null)
        {
            canUpdate = !canUpdateMetadataModel.MetadataId.Equals(Guid.Empty) &&
                        (canUpdateMetadataModel.File_LastWriteTime?.ToString("yyyy-MM-dd HH:mm:ss") != lastWriteTime.ToString("yyyy-MM-dd HH:mm:ss") ||
                         canUpdateMetadataModel.File_CreationTime?.ToString("yyyy-MM-dd HH:mm:ss") != creationTime.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        return canUpdate;
    }
      
    public async Task<List<string>> MetadataCanUpdatePathListAsync(List<string> paths)
    {
        string query = @"SELECT MetadataId, Path, File_LastWriteTime, File_CreationTime
                         FROM metadata 
                         WHERE path = ANY(@paths)";
        
        await using var conn = new NpgsqlConnection(_connectionString);

        var canUpdateMetadataModels = await conn
            .QueryAsync<CanUpdateMetadataModel>(query, new
            {
                paths
            });

        //get missing files that we can import
        List<string> pathsCanUpdate = paths
            .Where(path => !canUpdateMetadataModels.Any(model => string.Equals(model.Path, path)))
            .ToList();

        foreach (var model in canUpdateMetadataModels)
        {
            FileInfo fileInfo = new FileInfo(model.Path);
            if (!fileInfo.Exists)
            {
                continue;
            }
            
            bool canUpdate = !model.MetadataId.Equals(Guid.Empty) &&
                             (model.File_LastWriteTime?.ToString("yyyy-MM-dd HH:mm:ss") != fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss") ||
                              model.File_CreationTime?.ToString("yyyy-MM-dd HH:mm:ss") != fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"));
            if (canUpdate && !pathsCanUpdate.Contains(model.Path))
            {
                pathsCanUpdate.Add(model.Path);
            }
        }
        
        return pathsCanUpdate;
    }
    
    
    public async Task InsertOrUpdateMetadataAsync(MetadataInfo metadata, Guid albumId)
    {
        string query = @"
            INSERT INTO Metadata (MetadataId, 
                                  Path, 
                                  Title, 
                                  AlbumId, 
                                  MusicBrainzArtistId, 
                                  MusicBrainzDiscId, 
                                  MusicBrainzReleaseCountry, 
                                  MusicBrainzReleaseId, 
                                  MusicBrainzTrackId, 
                                  MusicBrainzReleaseStatus, 
                                  MusicBrainzReleaseType,
                                  MusicBrainzReleaseArtistId,
                                  MusicBrainzReleaseGroupId,
                                  Tag_Subtitle, 
                                  Tag_AlbumSort, 
                                  Tag_Comment, 
                                  Tag_Year, 
                                  Tag_Track, 
                                  Tag_TrackCount, 
                                  Tag_Disc, 
                                  Tag_DiscCount, 
                                  Tag_Lyrics, 
                                  Tag_Grouping, 
                                  Tag_BeatsPerMinute, 
                                  Tag_Conductor, 
                                  Tag_Copyright, 
                                  Tag_DateTagged, 
                                  Tag_AmazonId,
                                  Tag_ReplayGainTrackGain, 
                                  Tag_ReplayGainTrackPeak, 
                                  Tag_ReplayGainAlbumGain, 
                                  Tag_ReplayGainAlbumPeak, 
                                  Tag_InitialKey, 
                                  Tag_RemixedBy, 
                                  Tag_Publisher, 
                                  Tag_ISRC, 
                                  Tag_Length, 
                                  Tag_AcoustIdFingerPrint, 
                                  Tag_AcoustId,
                                  File_LastWriteTime,
                                  File_CreationTime,
                                  Tag_AllJsonTags)
            VALUES (@MetadataId, @path, @title, @albumId, 
                    @MusicBrainzArtistId, 
                    @MusicBrainzDiscId, 
                    @MusicBrainzReleaseCountry, 
                    @MusicBrainzReleaseId, 
                    @MusicBrainzTrackId, 
                    @MusicBrainzReleaseStatus, 
                    @MusicBrainzReleaseType, 
                    @MusicBrainzReleaseArtistId, 
                    @MusicBrainzReleaseGroupId,
                    @Tag_Subtitle, 
                    @Tag_AlbumSort, 
                    @Tag_Comment, 
                    @Tag_Year, 
                    @Tag_Track, 
                    @Tag_TrackCount, 
                    @Tag_Disc, 
                    @Tag_DiscCount, 
                    @Tag_Lyrics, 
                    @Tag_Grouping, 
                    @Tag_BeatsPerMinute, 
                    @Tag_Conductor, 
                    @Tag_Copyright, 
                    @Tag_DateTagged, 
                    @Tag_AmazonId, 
                    @Tag_ReplayGainTrackGain, 
                    @Tag_ReplayGainTrackPeak, 
                    @Tag_ReplayGainAlbumGain, 
                    @Tag_ReplayGainAlbumPeak, 
                    @Tag_InitialKey, @Tag_RemixedBy, 
                    @Tag_Publisher, 
                    @Tag_ISRC, 
                    @Tag_Length,
                    @Tag_AcoustIdFingerPrint,
                    @Tag_AcoustId,
                    @File_LastWriteTime,
                    @File_CreationTime,
                    @Tag_AllJsonTags::jsonb)
            ON CONFLICT (Path)
            DO UPDATE SET
                Title = EXCLUDED.Title,
                AlbumId = EXCLUDED.AlbumId,
                Tag_Subtitle = EXCLUDED.Tag_Subtitle,
                Tag_AlbumSort = EXCLUDED.Tag_AlbumSort,
                Tag_Comment = EXCLUDED.Tag_Comment,
                Tag_Year = EXCLUDED.Tag_Year,
                Tag_Track = EXCLUDED.Tag_Track,
                Tag_TrackCount = EXCLUDED.Tag_TrackCount,
                Tag_Disc = EXCLUDED.Tag_Disc,
                Tag_DiscCount = EXCLUDED.Tag_DiscCount,
                Tag_Lyrics = EXCLUDED.Tag_Lyrics,
                Tag_Grouping = EXCLUDED.Tag_Grouping,
                Tag_BeatsPerMinute = EXCLUDED.Tag_BeatsPerMinute,
                Tag_Conductor = EXCLUDED.Tag_Conductor,
                Tag_Copyright = EXCLUDED.Tag_Copyright,
                Tag_DateTagged = EXCLUDED.Tag_DateTagged,
                Tag_AmazonId = EXCLUDED.Tag_AmazonId,
                Tag_ReplayGainTrackGain = EXCLUDED.Tag_ReplayGainTrackGain,
                Tag_ReplayGainTrackPeak = EXCLUDED.Tag_ReplayGainTrackPeak,
                Tag_ReplayGainAlbumGain = EXCLUDED.Tag_ReplayGainAlbumGain,
                Tag_ReplayGainAlbumPeak = EXCLUDED.Tag_ReplayGainAlbumPeak,
                Tag_InitialKey = EXCLUDED.Tag_InitialKey,
                Tag_RemixedBy = EXCLUDED.Tag_RemixedBy,
                Tag_Publisher = EXCLUDED.Tag_Publisher,
                Tag_ISRC = EXCLUDED.Tag_ISRC,
                Tag_Length = EXCLUDED.Tag_Length,
                Tag_AcoustIdFingerPrint = COALESCE(metadata.Tag_AcoustIdFingerPrint, EXCLUDED.Tag_AcoustIdFingerPrint),
                Tag_AcoustId = COALESCE(metadata.Tag_AcoustId, EXCLUDED.Tag_AcoustId),
                File_LastWriteTime = EXCLUDED.File_LastWriteTime,
                File_CreationTime = EXCLUDED.File_CreationTime,
                Tag_AllJsonTags = EXCLUDED.Tag_AllJsonTags::jsonb,
                MusicBrainzArtistId = EXCLUDED.MusicBrainzArtistId, 
                MusicBrainzDiscId = EXCLUDED.MusicBrainzDiscId, 
                MusicBrainzReleaseCountry = EXCLUDED.MusicBrainzReleaseCountry, 
                MusicBrainzReleaseId = EXCLUDED.MusicBrainzReleaseId, 
                MusicBrainzTrackId = EXCLUDED.MusicBrainzTrackId, 
                MusicBrainzReleaseStatus = EXCLUDED.MusicBrainzReleaseStatus, 
                MusicBrainzReleaseType = EXCLUDED.MusicBrainzReleaseType,
                MusicBrainzReleaseArtistId = EXCLUDED.MusicBrainzReleaseArtistId,
                MusicBrainzReleaseGroupId = EXCLUDED.MusicBrainzReleaseGroupId
            RETURNING MetadataId";

        if (metadata.MetadataId.Equals(Guid.Empty))
        {
            metadata.MetadataId = Guid.NewGuid();
        }

        metadata.AlbumId = albumId;
        
        metadata.NonNullableValues();
        await using var conn = new NpgsqlConnection(_connectionString);
        
        metadata.MetadataId = await conn.QueryFirstOrDefaultAsync<Guid>(query, metadata);
        
    }
}