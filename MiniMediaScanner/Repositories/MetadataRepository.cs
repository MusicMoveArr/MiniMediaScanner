using MiniMediaScanner.Models;
using Npgsql;

namespace MiniMediaScanner.Repositories;

public class MetadataRepository
{
    private readonly string _connectionString;
    public MetadataRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public void UpdateMetadataFingerprint(string metadataId, string fingerprint, float duration)
    {
        string query = @"UPDATE metadata SET 
                                tag_acoustidfingerprint = @fingerprint,
                                tag_acoustidfingerprint_duration = @duration
                         WHERE MetadataId = cast(@id as uuid)";

        using var conn = new NpgsqlConnection(_connectionString);
        using var cmd = new NpgsqlCommand(query, conn);
        
        conn.Open();
        
        cmd.Parameters.AddWithValue("id", metadataId);
        cmd.Parameters.AddWithValue("fingerprint", fingerprint);
        cmd.Parameters.AddWithValue("duration", duration);

        var result = cmd.ExecuteNonQuery();
    }
    
    public void UpdateMetadataPath(string metadataId, string path)
    {
        string query = @"UPDATE metadata SET Path = @path WHERE MetadataId = cast(@id as uuid)";

        using var conn = new NpgsqlConnection(_connectionString);
        using var cmd = new NpgsqlCommand(query, conn);
        
        conn.Open();
        
        cmd.Parameters.AddWithValue("id", metadataId);
        cmd.Parameters.AddWithValue("path", path);

        var result = cmd.ExecuteNonQuery();
    }
    
    public List<string> GetMissingTracksByArtist(string artistName)
    {
        string query = @"WITH unique_tracks AS (
                         SELECT *
                         FROM (
                             select lower(re.title) as album_title, lower(ar.name) as artist_name, lower(track.title) as track_title, lower(re.status) as status,
                                    ROW_NUMBER() OVER (
                                        PARTITION BY lower(track.title), lower(re.title), lower(ar.name), lower(track.title)
                                    ) AS rn
                                    
                               FROM minimedia.public.musicbrainzartist ar
                                 JOIN minimedia.public.musicbrainzrelease re 
                                     ON re.musicbrainzartistid = CAST(ar.musicbrainzartistid AS TEXT)
                                     --AND lower(re.country) = lower(ar.country)
                                     --and lower(re.status) = 'official'
                                 JOIN minimedia.public.musicbrainzreleasetrack track 
                                     ON track.musicbrainzremotereleaseid = re.musicbrainzremotereleaseid
                         ) AS subquery
                             WHERE rn = 1
                     )
                     SELECT distinct ut.artist_name || ' - ' || ut.album_title || ' - ' || ut.track_title
                     FROM unique_tracks ut
 
                     left join artists a on lower(a.name) = ut.artist_name
                     left join albums album on 
	                     album.artistid = a.artistid 
	                     and lower(album.title) = ut.album_title
 
                     left join metadata m on
	                     (m.albumid = album.albumid --check by albumid
	                      and lower(m.title) = ut.track_title)
	                     or (lower(m.path) like '%/' || ut.album_title || '/%' --check album by path
	                        and lower(m.path) like '%/' || ut.artist_name || '/%' --check album by artist
	                         and lower(m.title) = ut.track_title)
	                     or (lower(m.path) like '%/' || ut.artist_name || '/%' --check by just the arist path
	                         and lower(m.title) = ut.track_title)
 
                     where ut.artist_name = @artistName
                     and ut.track_title not like '%(%'
                     and m.metadataid is null";

        using var conn = new NpgsqlConnection(_connectionString);
        using var cmd = new NpgsqlCommand(query, conn);
        cmd.CommandTimeout = 60;
        cmd.Parameters.AddWithValue("artistName", artistName.ToLower());
        
        conn.Open();

        using var reader = cmd.ExecuteReader();
        
        var result = new List<string>();
        while (reader.Read())
        {
            string trackName = reader.GetString(0);
            if (!string.IsNullOrWhiteSpace(trackName))
            {
                result.Add(trackName);
            }
        }

        return result;
    }
    
    public List<MetadataModel> PossibleDuplicateFiles(string artistName)
    {
        string query = @"select cast(m.MetadataId as text), m.Path, m.Title, cast(album.albumId as text)
                         from artists artist
                         join albums album on album.artistid = artist.artistid
                         join metadata m on m.albumid = album.albumid
                         where 
                         artist.""name"" = @artistName
                         and m.""path"" ~ '\([0-9]*\)\.([a-zA-Z0-9]{2,5})'";

        using var conn = new NpgsqlConnection(_connectionString);
        using var cmd = new NpgsqlCommand(query, conn);
        
        conn.Open();

        cmd.Parameters.AddWithValue("artistName", artistName);
        using var reader = cmd.ExecuteReader();
        
        var result = new List<MetadataModel>();
        while (reader.Read())
        {
            string metadataId = reader.GetString(0);
            string path = reader.GetString(1);
            string title = reader.GetString(2);
            string albumId = reader.GetString(3);
            result.Add(new MetadataModel()
            {
                MetadataId = metadataId,
                Path = path,
                Title = title,
                AlbumId = albumId
            });
        }

        return result;
    }
    
    public List<MetadataModel> GetAllMetadata(int offset, int limit)
    {
        string query = @$"select cast(m.MetadataId as text), m.path, m.title, cast(m.albumid as text), artist.""name"", album.title, m.tag_track
                          from artists artist
                          join albums album on album.artistid = artist.artistid
                          join metadata m on m.albumid = album.albumid 
                          order by m.""path"" asc 
                          OFFSET {offset}
                          LIMIT {limit}";

        using var conn = new NpgsqlConnection(_connectionString);
        using var cmd = new NpgsqlCommand(query, conn);
        
        conn.Open();

        using var reader = cmd.ExecuteReader();
        
        var result = new List<MetadataModel>();
        while (reader.Read())
        {
            result.Add(new MetadataModel()
            {
                MetadataId = reader.GetString(0),
                Path = reader.GetString(1),
                Title = reader.GetString(2),
                AlbumId = reader.GetString(3),
                ArtistName = reader.GetString(4),
                AlbumName = reader.GetString(5),
                Tag_Track = reader.GetInt32(6)
            });
        }

        return result;
    }
    
    public List<MetadataInfo> GetMissingMusicBrainzMetadataRecords(int offset, int limit)
    {
        string query = @$"SELECT cast(MetadataId as text), 
                                  Path, 
                                  Title, 
                                  cast(AlbumId as text), 
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
                                  Tag_AcoustIdFingerPrint_Duration
                        FROM minimedia.public.metadata m
                        WHERE (length(MusicBrainzArtistId) = 0 or 
                              length(MusicBrainzTrackId) = 0 or
                              length(MusicBrainzReleaseArtistId) = 0)
                              and length(tag_acoustidfingerprint) > 0
                        order by m.""path"" asc 
                        OFFSET {offset}
                        LIMIT {limit}";

        using var conn = new NpgsqlConnection(_connectionString);
        using var cmd = new NpgsqlCommand(query, conn);
        
        conn.Open();

        using var reader = cmd.ExecuteReader();
        var result = new List<MetadataInfo>();
        
        while (reader.Read())
        {
            result.Add(new MetadataInfo
            {
                MetadataId = reader.GetString(0),
                Path = reader.GetString(1),
                Title = reader.GetString(2),
                AlbumId = reader.GetString(3),
                MusicBrainzArtistId = reader.GetString(4),
                MusicBrainzDiscId = reader.GetString(5),
                MusicBrainzReleaseCountry = reader.GetString(6),
                MusicBrainzReleaseId = reader.GetString(7),
                MusicBrainzTrackId = reader.GetString(8),
                MusicBrainzReleaseStatus = reader.GetString(9),
                MusicBrainzReleaseType = reader.GetString(10),
                MusicBrainzReleaseArtistId = reader.GetString(11),
                MusicBrainzReleaseGroupId = reader.GetString(12),
                TagSubtitle = reader.GetString(13),
                TagAlbumSort = reader.GetString(14),
                TagComment = reader.GetString(15),
                TagYear = reader.GetInt32(16),
                TagTrack = reader.GetInt32(17),
                TagTrackCount = reader.GetInt32(18),
                TagDisc = reader.GetInt32(19),
                TagDiscCount = reader.GetInt32(20),
                TagLyrics = reader.GetString(21),
                TagGrouping = reader.GetString(22),
                TagBeatsPerMinute = reader.GetInt32(23),
                TagConductor = reader.GetString(24),
                TagCopyright = reader.GetString(25),
                TagDateTagged = reader.GetDateTime(26),
                TagAmazonId = reader.GetString(27),
                TagReplayGainTrackGain = reader.GetDouble(28),
                TagReplayGainTrackPeak = reader.GetDouble(29),
                TagReplayGainAlbumGain = reader.GetDouble(30),
                TagReplayGainAlbumPeak = reader.GetDouble(31),
                TagInitialKey = reader.GetString(32),
                TagRemixedBy = reader.GetString(33),
                TagPublisher = reader.GetString(34),
                TagISRC = reader.GetString(35),
                TagLength = reader.GetString(36),
                TagAcoustIdFingerPrint = reader.GetString(37),
                TagAcoustId = reader.GetString(38),
                TagAcoustIdFingerPrintDuration = reader.GetFloat(39),
            });
        }

        return result;
    }
    
    public List<MetadataModel> GetAllMetadataPathsByMissingFingerprint(int offset, int limit)
    {
        string query = @$"SELECT cast(m.MetadataId as text), m.Path
                        FROM minimedia.public.metadata m
                        where (length( m.tag_acoustidfingerprint) = 0 
                           or m.tag_acoustidfingerprint_duration = 0)
                           and length(m.musicbrainztrackid) = 0
                        order by m.""path"" asc 
                        OFFSET {offset}
                        LIMIT {limit}";

        using var conn = new NpgsqlConnection(_connectionString);
        using var cmd = new NpgsqlCommand(query, conn);
        
        conn.Open();

        using var reader = cmd.ExecuteReader();
        
        var result = new List<MetadataModel>();
        while (reader.Read())
        {
            string metadataId = reader.GetString(0);
            string path = reader.GetString(1);
            result.Add(new MetadataModel()
            {
                MetadataId = metadataId,
                Path = path
            });
        }

        return result;
    }
    
    public List<MetadataModel> GetMetadataByArtist(string artistName)
    {
        string query = @$"SELECT cast(m.MetadataId as text), 
                                 m.Path, 
                                 m.Title, 
                                 cast(m.AlbumId as text)
                        FROM minimedia.public.metadata m
                        JOIN albums album ON album.albumid = m.albumid
                        JOIN artists artist ON artist.artistid = album.artistid
                        where artist.name = @artistName";

        using var conn = new NpgsqlConnection(_connectionString);
        using var cmd = new NpgsqlCommand(query, conn);
        
        conn.Open();

        cmd.Parameters.AddWithValue("artistName", artistName);
        using var reader = cmd.ExecuteReader();
        
        var result = new List<MetadataModel>();
        while (reader.Read())
        {
            string metadataId = reader.GetString(0);
            string path = reader.GetString(1);
            string title = reader.GetString(2);
            string albumId = reader.GetString(3);
            result.Add(new MetadataModel()
            {
                MetadataId = metadataId,
                Path = path,
                Title = title,
                AlbumId = albumId
            });
        }

        return result;
    }
    
    public List<MetadataModel> GetMetadataByPath(string targetPath)
    {
        string query = @$"SELECT cast(m.MetadataId as text), 
                                 m.Path, 
                                 m.Title, 
                                 cast(m.AlbumId as text)
                        FROM minimedia.public.metadata m
                        where m.path = @path";

        using var conn = new NpgsqlConnection(_connectionString);
        using var cmd = new NpgsqlCommand(query, conn);
        
        conn.Open();

        cmd.Parameters.AddWithValue("path", targetPath);
        using var reader = cmd.ExecuteReader();
        
        var result = new List<MetadataModel>();
        while (reader.Read())
        {
            string metadataId = reader.GetString(0);
            string path = reader.GetString(1);
            string title = reader.GetString(2);
            string albumId = reader.GetString(3);
            result.Add(new MetadataModel()
            {
                MetadataId = metadataId,
                Path = path,
                Title = title,
                AlbumId = albumId
            });
        }

        return result;
    }
    
    public List<MetadataModel> GetMetadataByFileExtension(string fileExtension)
    {
        string query = @$"SELECT cast(m.MetadataId as text), 
                                 m.Path, 
                                 m.Title, 
                                 cast(m.AlbumId as text)
                        FROM minimedia.public.metadata m
                        where m.path like '%.' || @fileExtension";

        using var conn = new NpgsqlConnection(_connectionString);
        using var cmd = new NpgsqlCommand(query, conn);
        
        conn.Open();

        cmd.Parameters.AddWithValue("fileExtension", fileExtension);
        using var reader = cmd.ExecuteReader();
        
        var result = new List<MetadataModel>();
        while (reader.Read())
        {
            string metadataId = reader.GetString(0);
            string path = reader.GetString(1);
            string title = reader.GetString(2);
            string albumId = reader.GetString(3);
            result.Add(new MetadataModel()
            {
                MetadataId = metadataId,
                Path = path,
                Title = title,
                AlbumId = albumId
            });
        }

        return result;
    }
    
    public void DeleteMetadataRecords(List<string> metadataIds)
    {
        string query = @"DELETE FROM metadata WHERE metadataid = ANY(@id)";

        using var conn = new NpgsqlConnection(_connectionString);
        using var cmd = new NpgsqlCommand(query, conn);
    
        conn.Open();
    
        Guid[] uuidArray = metadataIds.Select(Guid.Parse).ToArray();
    
        // Pass the array as a parameter with Uuid | Array type
        cmd.Parameters.AddWithValue("id", NpgsqlTypes.NpgsqlDbType.Uuid | NpgsqlTypes.NpgsqlDbType.Array, uuidArray);

        cmd.ExecuteNonQuery();
    }
    
    
    
    
    public bool MetadataCanUpdate(string path, DateTime lastWriteTime, DateTime creationTime)
    {
        string query = @"SELECT cast(MetadataId as text), File_LastWriteTime, File_CreationTime 
                         FROM metadata 
                         WHERE path = @path
                         LIMIT 1";
        
        using var conn = new NpgsqlConnection(_connectionString);
        using var cmd = new NpgsqlCommand(query, conn);
        
        conn.Open();
        cmd.Parameters.AddWithValue("path", path);
        using var reader = cmd.ExecuteReader();
        bool canUpdate = true;
        
        if (reader.Read())
        {
            string metadataId = reader.GetString(0);
            DateTime dbLastWriteTime = reader.GetDateTime(1);
            DateTime dbCreationTime = reader.GetDateTime(2);

            canUpdate = !string.IsNullOrWhiteSpace(metadataId) &&
                        (dbLastWriteTime.ToString("yyyy-MM-dd HH:mm:ss") != lastWriteTime.ToString("yyyy-MM-dd HH:mm:ss") ||
                         dbCreationTime.ToString("yyyy-MM-dd HH:mm:ss") != creationTime.ToString("yyyy-MM-dd HH:mm:ss"));
            
        }
        return canUpdate;
    }
    
    public void InsertOrUpdateMetadata(MetadataInfo metadata, string filePath, Guid albumId)
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
            VALUES (@id, @path, @title, @albumId, 
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
                    @Tag_AllJsonTags)
            ON CONFLICT (Path)
            DO UPDATE SET
                Title = EXCLUDED.Title,
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
                Tag_AcoustIdFingerPrint = EXCLUDED.Tag_AcoustIdFingerPrint,
                Tag_AcoustId = EXCLUDED.Tag_AcoustId,
                File_LastWriteTime = EXCLUDED.File_LastWriteTime,
                File_CreationTime = EXCLUDED.File_CreationTime,
                Tag_AllJsonTags = EXCLUDED.Tag_AllJsonTags";

        Guid metadataId = Guid.NewGuid();
        
        metadata.NonNullableValues();
        using var conn = new NpgsqlConnection(_connectionString);
        using var cmd = new NpgsqlCommand(query, conn);
        
        conn.Open();
        cmd.Parameters.AddWithValue("id", metadataId);
        cmd.Parameters.AddWithValue("path", filePath);
        cmd.Parameters.AddWithValue("title", metadata.Title);
        cmd.Parameters.AddWithValue("albumId", albumId);
        cmd.Parameters.AddWithValue("MusicBrainzArtistId", metadata.MusicBrainzArtistId);
        cmd.Parameters.AddWithValue("MusicBrainzDiscId", metadata.MusicBrainzDiscId);
        cmd.Parameters.AddWithValue("MusicBrainzReleaseCountry", metadata.MusicBrainzReleaseCountry);
        cmd.Parameters.AddWithValue("MusicBrainzReleaseId", metadata.MusicBrainzReleaseId);
        cmd.Parameters.AddWithValue("MusicBrainzTrackId", metadata.MusicBrainzTrackId);
        cmd.Parameters.AddWithValue("MusicBrainzReleaseStatus", metadata.MusicBrainzReleaseStatus);
        cmd.Parameters.AddWithValue("MusicBrainzReleaseType", metadata.MusicBrainzReleaseType);
        cmd.Parameters.AddWithValue("MusicBrainzReleaseArtistId", metadata.MusicBrainzReleaseArtistId);
        cmd.Parameters.AddWithValue("MusicBrainzReleaseGroupId", metadata.MusicBrainzReleaseGroupId);
        cmd.Parameters.AddWithValue("Tag_Subtitle", metadata.TagSubtitle);
        cmd.Parameters.AddWithValue("Tag_AlbumSort", metadata.TagAlbumSort);
        cmd.Parameters.AddWithValue("Tag_Comment", metadata.TagComment);
        cmd.Parameters.AddWithValue("Tag_Year", metadata.TagYear);
        cmd.Parameters.AddWithValue("Tag_Track", metadata.TagTrack);
        cmd.Parameters.AddWithValue("Tag_TrackCount", metadata.TagTrackCount);
        cmd.Parameters.AddWithValue("Tag_Disc", metadata.TagDisc);
        cmd.Parameters.AddWithValue("Tag_DiscCount", metadata.TagDiscCount);
        cmd.Parameters.AddWithValue("Tag_Lyrics", metadata.TagLyrics);
        cmd.Parameters.AddWithValue("Tag_Grouping", metadata.TagGrouping);
        cmd.Parameters.AddWithValue("Tag_BeatsPerMinute", metadata.TagBeatsPerMinute);
        cmd.Parameters.AddWithValue("Tag_Conductor", metadata.TagConductor);
        cmd.Parameters.AddWithValue("Tag_Copyright", metadata.TagCopyright);
        cmd.Parameters.AddWithValue("Tag_DateTagged", metadata.TagDateTagged);
        cmd.Parameters.AddWithValue("Tag_AmazonId", metadata.TagAmazonId);
        cmd.Parameters.AddWithValue("Tag_ReplayGainTrackGain", metadata.TagReplayGainTrackGain);
        cmd.Parameters.AddWithValue("Tag_ReplayGainTrackPeak", metadata.TagReplayGainTrackPeak);
        cmd.Parameters.AddWithValue("Tag_ReplayGainAlbumGain", metadata.TagReplayGainAlbumGain);
        cmd.Parameters.AddWithValue("Tag_ReplayGainAlbumPeak", metadata.TagReplayGainAlbumPeak);
        cmd.Parameters.AddWithValue("Tag_InitialKey", metadata.TagInitialKey);
        cmd.Parameters.AddWithValue("Tag_RemixedBy", metadata.TagRemixedBy);
        cmd.Parameters.AddWithValue("Tag_Publisher", metadata.TagPublisher);
        cmd.Parameters.AddWithValue("Tag_ISRC", metadata.TagISRC);
        cmd.Parameters.AddWithValue("Tag_Length", metadata.TagLength);
        cmd.Parameters.AddWithValue("Tag_AcoustIdFingerPrint", metadata.TagAcoustIdFingerPrint);
        cmd.Parameters.AddWithValue("Tag_AcoustId", metadata.TagAcoustId);
        cmd.Parameters.AddWithValue("File_LastWriteTime", metadata.FileLastWriteTime);
        cmd.Parameters.AddWithValue("File_CreationTime", metadata.FileCreationTime);
        cmd.Parameters.AddWithValue("Tag_AllJsonTags", metadata.AllJsonTags);

        cmd.ExecuteNonQuery();
    }
}