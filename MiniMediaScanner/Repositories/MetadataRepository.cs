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
    
    public void UpdateMetadataFingerprint(string metadataId, string fingerprint, float duration,
        DateTime File_LastWriteTime, DateTime File_CreationTime)
    {
        string query = @"UPDATE metadata SET 
                                tag_acoustidfingerprint = @fingerprint,
                                tag_acoustidfingerprint_duration = @duration,
                                file_lastwritetime = @File_LastWriteTime,
                                file_creationtime = @File_CreationTime
                         WHERE MetadataId = cast(@id as uuid)";

        using var conn = new NpgsqlConnection(_connectionString);

        conn.Execute(query, new
        {
            id = metadataId,
            fingerprint,
            duration,
            File_LastWriteTime,
            File_CreationTime
        });
    }
    
    public void UpdateMetadataPath(string metadataId, string path)
    {
        string query = @"UPDATE metadata SET Path = @path WHERE MetadataId = cast(@id as uuid)";

        using var conn = new NpgsqlConnection(_connectionString);

        conn.Execute(query, new
        {
            id = metadataId,
            path
        });
    }
    
    
    
    public List<string> GetMissingTracksByArtist(List<string> artistNames)
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
                                    
                               FROM musicbrainzartist ar
                                 JOIN musicbrainzrelease re 
                                     ON re.musicbrainzartistid = ar.musicbrainzartistid
                                     --AND lower(re.country) = lower(ar.country)
                                      AND (lower(re.status) = 'official' OR LENGTH(re.status) = 0)
                                 JOIN musicbrainzreleasetrack track 
                                     ON track.musicbrainzremotereleaseid = re.musicbrainzremotereleaseid
                         ) AS subquery
                             WHERE rn = 1
                     )
                     SELECT distinct ut.artist_name || ' - ' || ut.album_title || ' - ' || ut.track_title
                     FROM unique_tracks ut
 
                     left join metadata m on {filter}
 
                     where ut.artist_name = lower(@artistName)
                     and m.metadataid is null";

        using var conn = new NpgsqlConnection(_connectionString);
        
        
        return conn
            .Query<string>(query, param: new {artistName = artistNames.First()}, commandTimeout: 9999)
            .ToList();
    }
    
    public List<MetadataModel> GetDuplicateFileVersions(string artistName)
    {
        string query = @"select m.MetadataId, m.Path, m.Title, album.albumId
                         from artists artist
                         join albums album on album.artistid = artist.artistid
                         join metadata m on m.albumid = album.albumid
                         where 
                         artist.""name"" = @artistName
                         and m.""path"" ~ '\([0-9]*\)\.([a-zA-Z0-9]{2,5})'";

        using var conn = new NpgsqlConnection(_connectionString);
        
        return conn.Query<MetadataModel>(query, 
                new
                {
                    artistName
                })
            .ToList();
    }
    public List<DuplicateFileExtensionModel> GetDuplicateFileExtensions(string artistName)
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

        using var conn = new NpgsqlConnection(_connectionString);
        
        return conn.Query<DuplicateFileExtensionModel>(query, 
                new
                {
                    artistName
                })
            .ToList();
    }
    public List<DuplicateAlbumFileNameModel> GetDuplicateAlbumFileNames(string artistName)
    {
        string query = @"WITH duplicates AS (
                             SELECT 
                                 m.MetadataId,
                                 m.Path,
                                 m.Title,
                                 album.AlbumId,
                                 REGEXP_REPLACE(m.Path, '^.*/([^/]*/[^/]+)$', '\1', 'g') AS FileName,
                                 COUNT(*) OVER (PARTITION BY album.albumId, REGEXP_REPLACE(m.Path, '^.*/([^/]*/[^/]+)$', '\1', 'g')) AS duplicate_count
                             FROM artists artist
                             JOIN albums album ON album.artistid = artist.artistid
                             JOIN metadata m ON m.albumid = album.albumid
                             WHERE LOWER(artist.name) = LOWER(@artistName)
                         )
                         SELECT MetadataId, Path, Title, AlbumId, FileName, duplicate_count
                         FROM duplicates
                         WHERE duplicate_count > 1
                         ORDER BY albumId, FileName, Path";

        using var conn = new NpgsqlConnection(_connectionString);
        
        return conn.Query<DuplicateAlbumFileNameModel>(query, 
                new
                {
                    artistName
                }, commandTimeout: 120)
            .ToList();
    }
    
    public List<MetadataInfo> GetMissingMusicBrainzMetadataRecords(string artistName)
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

        using var conn = new NpgsqlConnection(_connectionString);

        return conn
            .Query<MetadataInfo>(query, new
            {
                artistName
            }).ToList();
    }
    
    public List<MetadataInfo> GetMissingSpotifyMetadataRecords(string artistName)
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

        using var conn = new NpgsqlConnection(_connectionString);

        return conn
            .Query<MetadataInfo>(query, new
            {
                artistName
            }).ToList();
    }
    
    public List<MetadataInfo> GetMetadataByTagRecords(string artistName, List<String> tagNames)
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

        using var conn = new NpgsqlConnection(_connectionString);

        return conn
            .Query<MetadataInfo>(query, new
            {
                artistName,
                tagNames
            }, commandTimeout: 120).ToList();
    }
    
    
    public List<MetadataInfo> GetMetadataByTagValueRecords(string artistName, string tagName, string value)
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

        using var conn = new NpgsqlConnection(_connectionString);

        return conn
            .Query<MetadataInfo>(query, new
            {
                artistName,
                tagName,
                value
            }, commandTimeout: 120).ToList();
    }
    
    public List<MetadataModel> GetAllMetadataPathsByMissingFingerprint(string artistName)
    {
        string query = @$"SELECT m.MetadataId, m.Path
                        FROM metadata m
                        JOIN albums album ON album.albumid = m.albumid
                        JOIN artists artist ON artist.artistid = album.artistid
                        where lower(artist.name) = lower(@artistName)
                        and (length( m.tag_acoustidfingerprint) = 0
                            or m.tag_acoustidfingerprint_duration = 0)";

        using var conn = new NpgsqlConnection(_connectionString);
        
        return conn.Query<MetadataModel>(query, new
        {
            artistName
        }).ToList();
    }
    
    public List<MetadataModel> GetMetadataByArtist(string artistName)
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
                                 m.Tag_AllJsonTags
                        FROM metadata m
                        JOIN albums album ON album.albumid = m.albumid
                        JOIN artists artist ON artist.artistid = album.artistid
                        where lower(artist.name) = lower(@artistName)";

        using var conn = new NpgsqlConnection(_connectionString);
        
        return conn.Query<MetadataModel>(query, new
        {
            artistName
        }).ToList();
    }
    
    public List<MetadataPathCoverModel> GetFolderPathsByArtistForCovers(string artistName, string album)
    {
        string query = @$"SELECT distinct regexp_replace(path, '[^/]+$', '') AS FolderPath, 
                                 m.MusicBrainzReleaseId,
                                 artist.name as ArtistName,
                                 album.title AS AlbumName
                          FROM artists artist
                          JOIN albums album ON album.artistid = artist.artistid
                          JOIN metadata m ON m.albumid = album.albumid
                          where lower(artist.name) = lower(@artistName)
                                and length(m.MusicBrainzReleaseId) > 0
                                and (length(@album) = 0 or @album is null or lower(album.title) = lower(@album))";

        using var conn = new NpgsqlConnection(_connectionString);
        
        return conn.Query<MetadataPathCoverModel>(query, new
        {
            artistName,
            album
        }).ToList();
    }
    
    public List<MetadataModel> GetMetadataByPath(string targetPath)
    {
        string query = @$"SELECT m.MetadataId, 
                                 m.Path, 
                                 m.Title, 
                                 m.AlbumId,
                                 m.tag_acoustidfingerprint
                        FROM metadata m
                        where m.path = @path";

        using var conn = new NpgsqlConnection(_connectionString);

        return conn
            .Query<MetadataModel>(query, new
            {
                path = targetPath
            }).ToList();
    }
    
    public List<string> GetPathByLikePath(string path)
    {
        string query = @$"SELECT m.Path
                        FROM metadata m
                        where m.path like @path || '%'";

        using var conn = new NpgsqlConnection(_connectionString);

        return conn
            .Query<string>(query, new
            {
                path
            }).ToList();
    }
    
    public List<MetadataModel> GetMetadataByFileExtension(string fileExtension)
    {
        string query = @$"SELECT m.MetadataId, 
                                 m.Path, 
                                 m.Title, 
                                 m.AlbumId
                        FROM metadata m
                        where m.path like '%.' || @fileExtension";

        using var conn = new NpgsqlConnection(_connectionString);

        return conn
            .Query<MetadataModel>(query, new
            {
                fileExtension
            }).ToList();
    }
    
    public void DeleteMetadataRecords(List<string> metadataIds)
    {
        string query = @"DELETE FROM metadata WHERE metadataid = ANY(@id)";

        using var conn = new NpgsqlConnection(_connectionString);
        
        conn.Execute(query, new
        {
            id = metadataIds.Select(id => Guid.Parse(id)).ToList()
        });
    }
    
    public bool MetadataCanUpdate(string path, DateTime lastWriteTime, DateTime creationTime)
    {
        string query = @"SELECT MetadataId, File_LastWriteTime, File_CreationTime 
                         FROM metadata 
                         WHERE path = @path
                         LIMIT 1";
        
        using var conn = new NpgsqlConnection(_connectionString);

        CanUpdateMetadataModel? canUpdateMetadataModel = conn
            .Query<CanUpdateMetadataModel>(query, new
            {
                path
            }).FirstOrDefault();

        bool canUpdate = true;
        if (canUpdateMetadataModel != null)
        {
            canUpdate = !canUpdateMetadataModel.MetadataId.Equals(Guid.Empty) &&
                        (canUpdateMetadataModel.File_LastWriteTime?.ToString("yyyy-MM-dd HH:mm:ss") != lastWriteTime.ToString("yyyy-MM-dd HH:mm:ss") ||
                         canUpdateMetadataModel.File_CreationTime?.ToString("yyyy-MM-dd HH:mm:ss") != creationTime.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        return canUpdate;
    }
    
    public void InsertOrUpdateMetadata(MetadataInfo metadata, Guid albumId)
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
        using var conn = new NpgsqlConnection(_connectionString);
        
        metadata.MetadataId = conn.Query<Guid>(query, metadata).FirstOrDefault();
        
    }
}