using Dapper;
using Npgsql;

namespace MiniMediaScanner.Repositories;

public class DeezerRepository
{
    private readonly string _connectionString;
    public DeezerRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<DateTime?> GetArtistLastSyncTimeAsync(long artistId)
    {
        string query = @"SELECT lastsynctime FROM deezer_artist WHERE ArtistId = @id";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.ExecuteScalarAsync<DateTime>(query, new
        {
            id = artistId
        });
    }
    
    public async Task<DateTime?> SetArtistLastSyncTimeAsync(long artistId)
    {
        string query = @"UPDATE deezer_artist SET lastsynctime = @lastsynctime WHERE ArtistId = @id";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.ExecuteScalarAsync<DateTime>(query, new
        {
            id = artistId,
            lastsynctime = DateTime.Now
        });
    }
    
    public async Task UpsertArtistAsync(long artistId, string name, int nbAlbum, int nbFan, bool radio, string type)
    {
        string query = @"
            INSERT INTO deezer_artist (ArtistId, 
                                  Name, 
                                  NbAlbum, 
                                  NbFan,
                                  Radio,
                                  Type,
                                  lastsynctime)
            VALUES (@artistId, @name, @nbAlbum, @nbFan, @radio, @type, @lastsynctime)
            ON CONFLICT (ArtistId)
            DO UPDATE SET
                Name = EXCLUDED.Name,
                nbAlbum = EXCLUDED.nbAlbum,
                nbFan = EXCLUDED.nbFan,
                radio = EXCLUDED.radio,
                type = EXCLUDED.type";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        await conn.ExecuteAsync(query, param: new
        {
            artistId,
            name,
            nbAlbum,
            nbFan,
            radio,
            type,
            lastsynctime = new DateTime(2000, 1, 1)
        });
    }
    
    public async Task UpsertArtistImageLinkAsync(long artistId, string href, string type)
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            return;
        }
        
        string query = @"
            INSERT INTO deezer_artist_image_link (ArtistId, 
                                  href, 
                                  type)
            VALUES (@artistId, @href, @type)
            ON CONFLICT (ArtistId, type)
            DO UPDATE SET
                href = EXCLUDED.href";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        await conn.ExecuteAsync(query, param: new
        {
            artistId,
            href,
            type
        });
    }
    
    
    public async Task UpsertAlbumAsync(long albumId,
        long artistId,
        string title,
        string md5Image,
        int genreId,
        int fans,
        string releaseDate,
        string recordType,
        bool explicitLyrics,
        string type,
        int explicitContentLyrics,
        int explicitContentCover,
        string upc,
        string label,
        int nbTracks,
        int duration,
        bool available)
    {
        string query = @"
            INSERT INTO deezer_album (AlbumId, 
                                  ArtistId, 
                                  Title, 
                                  md5Image, 
                                  genreId, 
                                  fans, 
                                  releaseDate, 
                                  recordType, 
                                  explicitLyrics, 
                                  type,
                                  explicitContentLyrics,
                                  explicitContentCover,
                                  upc,
                                  label,
                                  nbTracks,
                                  duration,
                                  available)
            VALUES (@albumId, @artistId, @title, @md5Image, 
                    @genreId, @fans, @releaseDate, 
                    @recordType, @explicitLyrics, @type,
                    @explicitContentLyrics, @explicitContentCover,
                    @upc, @label, @nbTracks, @duration, @available)
            ON CONFLICT (AlbumId, ArtistId)
            DO UPDATE SET
                title = EXCLUDED.title,
                md5Image = EXCLUDED.md5Image,
                genreId = EXCLUDED.genreId,
                fans = EXCLUDED.fans,
                releaseDate = EXCLUDED.releaseDate,
                recordType = EXCLUDED.recordType,
                explicitLyrics = EXCLUDED.explicitLyrics,
                type = EXCLUDED.type,
                explicitContentLyrics = EXCLUDED.explicitContentLyrics,
                explicitContentCover = EXCLUDED.explicitContentCover,
                upc = EXCLUDED.upc,
                label = EXCLUDED.label,
                nbTracks = EXCLUDED.nbTracks,
                duration = EXCLUDED.duration,
                available = EXCLUDED.available";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        await conn.ExecuteAsync(query, param: new
        {
            albumId,
            artistId,
            title,
            md5Image,
            genreId,
            fans,
            releaseDate,
            recordType,
            explicitLyrics,
            type,
            explicitContentLyrics,
            explicitContentCover,
            upc,
            label,
            nbTracks,
            duration,
            available
        });
    }
    
    public async Task UpsertAlbumArtistIdAsync(long albumId, long artistId, string role)
    {
        string query = @"
            INSERT INTO deezer_album_artist (AlbumId, ArtistId, Role)
            VALUES (@albumId, @artistId, @role)
            ON CONFLICT (AlbumId, ArtistId, Role)
            DO NOTHING";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        await conn.ExecuteAsync(query, param: new
        {
            albumId,
            artistId,
            role
        });
    }
    
    public async Task UpsertAlbumImageLinkAsync(long albumId, string href, string type)
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            return;
        }
        
        string query = @"
            INSERT INTO deezer_album_image_link (AlbumId, 
                                  href, 
                                  type)
            VALUES (@albumId, @href, @type)
            ON CONFLICT (AlbumId, type)
            DO UPDATE SET
                href = EXCLUDED.href";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        await conn.ExecuteAsync(query, param: new
        {
            albumId,
            href,
            type
        });
    }
    
    
    public async Task UpsertTrackAsync(
        long trackId,
        long albumId,
        long artistId,
        bool readable,
        string title,
        string titleShort,
        string titleVersion,
        string isrc,
        int duration,
        int trackPosition,
        int diskNumber,
        int rank,
        string releaseDate,
        bool explicitLyrics,
        int explicitContentLyrics,
        int explicitContentCover,
        string preview,
        float bpm,
        float gain,
        string md5Image,
        string trackToken,
        string type)
    {
        string query = @"
            INSERT INTO deezer_track (TrackId, 
                                  Readable, 
                                  Title, 
                                  TitleShort, 
                                  TitleVersion, 
                                  ISRC, 
                                  Duration, 
                                  TrackPosition, 
                                  DiskNumber, 
                                  Rank, 
                                  ReleaseDate, 
                                  ExplicitLyrics,
                                  ExplicitContentLyrics,
                                  ExplicitContentCover,
                                  Preview,
                                  BPM,
                                  Gain,
                                  Md5Image,
                                  TrackToken,
                                  ArtistId,
                                  AlbumId,
                                  Type)
            VALUES (@trackId, @readable, @title, @titleShort, 
                    @titleVersion, @isrc, @duration, 
                    @trackPosition, @diskNumber, @rank, 
                    @releaseDate, @explicitLyrics, @explicitContentLyrics,
                    @explicitContentCover, @preview, @bpm, @gain, @md5Image,
                    @trackToken, @artistId, @albumId, @type)
            ON CONFLICT (TrackId, ArtistId, AlbumId)
            DO UPDATE SET
                readable = EXCLUDED.readable,
                title = EXCLUDED.title,
                titleShort = EXCLUDED.titleShort,
                titleVersion = EXCLUDED.titleVersion,
                isrc = EXCLUDED.isrc,
                duration = EXCLUDED.duration,
                trackPosition = EXCLUDED.trackPosition,
                diskNumber = EXCLUDED.diskNumber,
                rank = EXCLUDED.rank,
                releaseDate = EXCLUDED.releaseDate,
                explicitLyrics = EXCLUDED.explicitLyrics,
                explicitContentLyrics = EXCLUDED.explicitContentLyrics,
                explicitContentCover = EXCLUDED.explicitContentCover,
                preview = EXCLUDED.preview,
                bpm = EXCLUDED.bpm,
                gain = EXCLUDED.gain,
                md5Image = EXCLUDED.md5Image,
                trackToken = EXCLUDED.trackToken,
                artistId = EXCLUDED.artistId,
                albumId = EXCLUDED.albumId,
                type = EXCLUDED.type";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        await conn.ExecuteAsync(query, param: new
        {
            trackId,
            readable,
            title,
            titleShort,
            titleVersion,
            isrc,
            duration,
            trackPosition,
            diskNumber, 
            rank,
            releaseDate,
            explicitLyrics,
            explicitContentLyrics,
            explicitContentCover,
            preview,
            bpm,
            gain,
            md5Image,
            trackToken,
            artistId,
            albumId,
            type
        });
    }
    
    public async Task UpsertTrackArtistIdAsync(long trackId, long artistId, long albumId)
    {
        string query = @"
            INSERT INTO deezer_track_artist (TrackId, ArtistId, AlbumId)
            VALUES (@trackId, @artistId, @albumId)
            ON CONFLICT (TrackId, ArtistId, AlbumId)
            DO NOTHING";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        await conn.ExecuteAsync(query, param: new
        {
            trackId,
            artistId,
            albumId
        });
    }
    
    public async Task UpsertGenreAsync(long genreId, string name, string picture, string type)
    {
        string query = @"
            INSERT INTO deezer_genre (GenreId, Name, Picture, Type)
            VALUES (@genreId, @name, @picture, @type)
            ON CONFLICT (GenreId, Type)
            DO NOTHING";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        await conn.ExecuteAsync(query, param: new
        {
            genreId,
            name,
            picture,
            type
        });
    }
    public async Task UpsertAlbumGenreAsync(long albumId, long genreId)
    {
        string query = @"
            INSERT INTO deezer_album_genre (AlbumId, GenreId)
            VALUES (@albumId, @genreId)
            ON CONFLICT (AlbumId, GenreId)
            DO NOTHING";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        await conn.ExecuteAsync(query, param: new
        {
            albumId,
            genreId
        });
    }
    
    public async Task<int> GetAlbumTrackCountAsync(long albumId, long artistId)
    {
        string query = @"SELECT count(track.trackid)
                         FROM deezer_album album
                         join deezer_track track on track.albumid = album.albumid
                         where album.albumid = @albumId and album.artistId = @artistId
                         limit 1";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn
            .ExecuteScalarAsync<int>(query,
                param: new
                {
                    albumId,
                    artistId
                });
    }
    
    public async Task<List<int>> GetAllDeezerArtistIdsAsync()
    {
        string query = @"SELECT artistid FROM deezer_artist order by name asc";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        return (await conn
                .QueryAsync<int>(query))
            .ToList();
    }
    
    public async Task<List<string>> GetTrackArtistsAsync(long trackId, int orderByArtistId)
    {
        string query = @"SELECT artist.name
                         FROM deezer_track_artist tta
                         join deezer_artist artist on artist.artistid = tta.artistid
                         where tta.trackid = @trackId
                         order by CASE WHEN artist.artistid = @orderByArtistId THEN 0 ELSE 1 end asc";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        return (await conn
                .QueryAsync<string>(query,
                    param: new
                    {
                        trackId,
                        orderByArtistId
                    }))
            .ToList();
    }
    public async Task<bool> ArtistExistsByIdAsync(long artistId)
    {
        string query = @"SELECT 1
                         FROM deezer_artist artist
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
    
    public async Task<string?> GetHighestQualityArtistCoverUrlAsync(long artistId)
    {
        string query = @"SELECT dail.href
                         FROM deezer_artist_image_link dail
                         where dail.artistid = @artistId
                         and dail.type != 'picture'
 
                         order by 
                             CASE WHEN dail.type = 'xl' THEN 0 
                                  WHEN dail.type = 'big' THEN 1
                                  WHEN dail.type = 'medium' THEN 2
                                  WHEN dail.type = 'small' THEN 3
                             ELSE 4 end asc
                         limit 1";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn
            .QueryFirstOrDefaultAsync<string>(query,
                param: new
                {
                    artistId
                });
    }
    
    public async Task<string?> GetHighestQualityAlbumCoverUrlAsync(long artistId, string albumName)
    {
        string query = @"SELECT dail.href
                         FROM deezer_album_image_link dail
                         join deezer_album album on album.albumid = dail.albumid and lower(album.title) = lower(@albumName)
                         join deezer_album_artist saa on saa.artistid = @artistId and saa.albumid = dail.albumid
                         where dail.type != 'picture'
 
                         order by 
                             CASE WHEN dail.type = 'xl' THEN 0 
                                  WHEN dail.type = 'big' THEN 1
                                  WHEN dail.type = 'medium' THEN 2
                                  WHEN dail.type = 'small' THEN 3
                             ELSE 4 end asc
                         limit 1";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn
            .QueryFirstOrDefaultAsync<string>(query,
                param: new
                {
                    artistId,
                    albumName
                });
    }
}