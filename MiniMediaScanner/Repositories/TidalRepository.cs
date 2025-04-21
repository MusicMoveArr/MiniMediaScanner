using Dapper;
using MiniMediaScanner.Models.Tidal;
using Npgsql;

namespace MiniMediaScanner.Repositories;

public class TidalRepository
{
    private readonly string _connectionString;
    public TidalRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<DateTime?> GetArtistLastSyncTimeAsync(int artistId)
    {
        string query = @"SELECT lastsynctime FROM tidal_artist WHERE ArtistId = @id";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.ExecuteScalarAsync<DateTime>(query, new
        {
            id = artistId
        });
    }
    
    public async Task UpsertArtistAsync(int artistId, string name, float popularity)
    {
        string query = @"
            INSERT INTO tidal_artist (ArtistId, 
                                  Name, 
                                  Popularity, 
                                  lastsynctime)
            VALUES (@artistId, @name, @popularity, @lastsynctime)
            ON CONFLICT (ArtistId)
            DO UPDATE SET
                Name = EXCLUDED.Name,
                Popularity = EXCLUDED.Popularity,
                lastsynctime = EXCLUDED.lastsynctime";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        await conn.ExecuteAsync(query, param: new
        {
            artistId,
            name,
            popularity,
            lastsynctime = DateTime.Now
        });
    }
    
    public async Task UpsertArtistImageLinkAsync(int artistId, string href, int metaWidth, int metaHeight)
    {
        string query = @"
            INSERT INTO tidal_artist_image_link (ArtistId, 
                                  href, 
                                  meta_width, 
                                  meta_height)
            VALUES (@artistId, @href, @metaWidth, @metaHeight)
            ON CONFLICT (ArtistId, meta_width, meta_height)
            DO UPDATE SET
                href = EXCLUDED.href,
                meta_width = EXCLUDED.meta_width,
                meta_height = EXCLUDED.meta_height";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        await conn.ExecuteAsync(query, param: new
        {
            artistId,
            href,
            metaWidth,
            metaHeight
        });
    }
    
    public async Task UpsertArtistExternalLinkAsync(int artistId, string href, string metaType)
    {
        string query = @"
            INSERT INTO tidal_artist_external_link (ArtistId, href, meta_type)
            VALUES (@artistId, @href, @metaType)
            ON CONFLICT (ArtistId, href, meta_type)
            DO NOTHING";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        await conn.ExecuteAsync(query, param: new
        {
            artistId,
            href,
            metaType
        });
    }
    
    public async Task UpsertAlbumAsync(int albumId,
        int artistId,
        string title,
        string barcodeId,
        int numberOfVolumes,
        int numberOfItems,
        string duration,
        bool _explicit,
        string releaseDate,
        string copyright,
        float popularity,
        string availability,
        string mediaTags)
    {
        string query = @"
            INSERT INTO tidal_album (AlbumId, 
                                  ArtistId, 
                                  Title, 
                                  BarcodeId, 
                                  NumberOfVolumes, 
                                  NumberOfItems, 
                                  Duration, 
                                  Explicit, 
                                  ReleaseDate, 
                                  Copyright, 
                                  Popularity, 
                                  Availability, 
                                  MediaTags)
            VALUES (@albumId, @artistId, @title, @barcodeId, 
                    @numberOfVolumes, @numberOfItems, @duration, 
                    @_explicit, @releaseDate, @copyright, 
                    @popularity, @availability, @mediaTags)
            ON CONFLICT (AlbumId, ArtistId)
            DO UPDATE SET
                title = EXCLUDED.title,
                barcodeId = EXCLUDED.barcodeId,
                numberOfVolumes = EXCLUDED.numberOfVolumes,
                duration = EXCLUDED.duration,
                explicit = EXCLUDED.explicit,
                releaseDate = EXCLUDED.releaseDate,
                copyright = EXCLUDED.copyright,
                popularity = EXCLUDED.popularity,
                availability = EXCLUDED.availability,
                mediaTags = EXCLUDED.mediaTags";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        await conn.ExecuteAsync(query, param: new
        {
            albumId,
            artistId,
            title,
            barcodeId,
            numberOfVolumes,
            numberOfItems,
            duration,
            _explicit,
            releaseDate,
            copyright,
            popularity,
            availability,
            mediaTags
        });
    }
    
    
    public async Task UpsertAlbumImageLinkAsync(int albumId, string href, int metaWidth, int metaHeight)
    {
        string query = @"
            INSERT INTO tidal_album_image_link (AlbumId, 
                                  href, 
                                  meta_width, 
                                  meta_height)
            VALUES (@albumId, @href, @metaWidth, @metaHeight)
            ON CONFLICT (AlbumId, meta_width, meta_height)
            DO UPDATE SET
                href = EXCLUDED.href,
                meta_width = EXCLUDED.meta_width,
                meta_height = EXCLUDED.meta_height";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        await conn.ExecuteAsync(query, param: new
        {
            albumId,
            href,
            metaWidth,
            metaHeight
        });
    }
    
    public async Task UpsertAlbumExternalLinkAsync(int albumId, string href, string metaType)
    {
        string query = @"
            INSERT INTO tidal_album_external_link (AlbumId, href, meta_type)
            VALUES (@albumId, @href, @metaType)
            ON CONFLICT (albumId, href, meta_type)
            DO NOTHING";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        await conn.ExecuteAsync(query, param: new
        {
            albumId,
            href,
            metaType
        });
    }
    
    public async Task UpsertTrackAsync(int trackId,
        int albumId,
        string title,
        string isrc,
        string duration,
        string copyright,
        bool _explicit,
        float popularity,
        string availability,
        string mediaTags,
        int volumeNumber,
        int trackNumber)
    {
        string query = @"
            INSERT INTO tidal_track (TrackId, 
                                  AlbumId, 
                                  Title, 
                                  ISRC, 
                                  Duration, 
                                  Copyright, 
                                  Explicit, 
                                  Popularity, 
                                  Availability, 
                                  MediaTags, 
                                  VolumeNumber, 
                                  TrackNumber)
            VALUES (@trackId, @albumId, @title, @isrc, 
                    @duration, @copyright, @_explicit, 
                    @popularity, @availability, @mediaTags, 
                    @volumeNumber, @trackNumber)
            ON CONFLICT (TrackId, AlbumId)
            DO UPDATE SET
                Title = EXCLUDED.Title,
                ISRC = EXCLUDED.ISRC,
                Duration = EXCLUDED.Duration,
                Copyright = EXCLUDED.Copyright,
                Explicit = EXCLUDED.Explicit,
                Popularity = EXCLUDED.Popularity,
                Availability = EXCLUDED.Availability,
                MediaTags = EXCLUDED.MediaTags,
                VolumeNumber = EXCLUDED.VolumeNumber,
                TrackNumber = EXCLUDED.TrackNumber";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        await conn.ExecuteAsync(query, param: new
        {
            trackId,
            albumId,
            title,
            isrc,
            duration,
            copyright,
            _explicit,
            popularity,
            availability, 
            mediaTags,
            volumeNumber,
            trackNumber
        });
    }
    
    public async Task UpsertTrackExternalLinkAsync(int trackId, string href, string metaType)
    {
        string query = @"
            INSERT INTO tidal_track_external_link (TrackId, href, meta_type)
            VALUES (@trackId, @href, @metaType)
            ON CONFLICT (TrackId, href, meta_type)
            DO NOTHING";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        await conn.ExecuteAsync(query, param: new
        {
            trackId,
            href,
            metaType
        });
    }
    
    public async Task UpsertTrackImageLinkAsync(int trackId, string href, int metaWidth, int metaHeight)
    {
        string query = @"
            INSERT INTO tidal_track_image_link (TrackId, 
                                  href, 
                                  meta_width, 
                                  meta_height)
            VALUES (@trackId, @href, @metaWidth, @metaHeight)
            ON CONFLICT (TrackId, meta_width, meta_height)
            DO UPDATE SET
                href = EXCLUDED.href,
                meta_width = EXCLUDED.meta_width,
                meta_height = EXCLUDED.meta_height";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        await conn.ExecuteAsync(query, param: new
        {
            trackId,
            href,
            metaWidth,
            metaHeight
        });
    }
    
    public async Task<bool> TidalAlbumIdExistsAsync(int albumId, int artistId)
    {
        string query = @"SELECT 1
                         FROM tidal_album album
                         where album.albumid = @albumId and album.artistId = @artistId
                         limit 1";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn
            .ExecuteScalarAsync<int?>(query,
                param: new
                {
                    albumId,
                    artistId
                })) == 1;
    }
    
    public async Task<List<int>> GetAllTidalArtistIdsAsync()
    {
        string query = @"SELECT artistid FROM tidal_artist order by name asc";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        return (await conn
                .QueryAsync<int>(query))
            .ToList();
    }
    public async Task UpsertTrackArtistIdAsync(int trackId, int artistId)
    {
        string query = @"
            INSERT INTO tidal_track_artist (TrackId, ArtistId)
            VALUES (@trackId, @artistId)
            ON CONFLICT (TrackId, ArtistId)
            DO NOTHING";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        await conn.ExecuteAsync(query, param: new
        {
            trackId,
            artistId
        });
    }
    
    public async Task UpsertProviderAsync(int providerId, string name)
    {
        string query = @"
            INSERT INTO tidal_provider (ProviderId, Name)
            VALUES (@providerId, @name)
            ON CONFLICT (ProviderId)
            DO NOTHING";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        await conn.ExecuteAsync(query, param: new
        {
            providerId,
            name
        });
    }
    
    public async Task UpsertTrackProviderAsync(int trackId, int providerId)
    {
        string query = @"
            INSERT INTO tidal_track_provider (TrackId, ProviderId)
            VALUES (@trackId, @providerId)
            ON CONFLICT (TrackId, ProviderId)
            DO NOTHING";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        await conn.ExecuteAsync(query, param: new
        {
            trackId,
            providerId
        });
    }
    
    public async Task<List<TidalTrackModel>> GetTrackByArtistIdAsync(int artistId, string albumName, string trackName)
    {
        string query = @"select
                             track.title As TrackName,
                             track.TrackId,
                             track.AlbumId,
                             track.VolumeNumber AS DiscNumber,
                             cast(track.duration::interval as text) as Duration,
                             track.Explicit,
                             trackExtLink.href AS TrackHref,
                             track.TrackNumber,
                             track.isrc AS TrackISRC,
                             album.barcodeid AS AlbumUPC,
                             album.ReleaseDate,
                             album.NumberOfItems AS TotalTracks,
                             album.copyright as Copyright,
                             album.title as AlbumName,
                             albumExtLink.href as AlbumHref,
                             artistExtLink.Href as ArtistHref,
                             artist.name as ArtistName,
                             artist.artistid as ArtistId
                         from tidal_track track
                         join tidal_album album on album.albumid = track.albumid
                         join tidal_track_artist track_artist on track_artist.trackid = track.trackid
                         join tidal_artist artist on artist.artistid = track_artist.artistid or artist.artistid = @artistId
                         left join tidal_track_external_link trackExtLink on trackExtLink.trackid = track.trackid and trackExtLink.meta_type = 'TIDAL_SHARING'
                         left join tidal_album_external_link albumExtLink on albumExtLink.albumid = album.albumid and albumExtLink.meta_type = 'TIDAL_SHARING'
                         left join tidal_artist_external_link artistExtLink on artistExtLink.artistid = artist.artistid and artistExtLink.meta_type = 'TIDAL_SHARING'
                         where   (length(@albumName) = 0 OR similarity(lower(album.title), lower(@albumName)) >= 0.8)
	                         and (length(@trackName) = 0 OR similarity(lower(track.title), lower(@trackName)) >= 0.8)";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        return (await conn
            .QueryAsync<TidalTrackModel>(query,
                param: new
                {
                    artistId,
                    albumName,
                    trackName
                }))
            .ToList();
    }
    
    public async Task<List<string>> GetTrackArtistsAsync(int trackId, int orderByArtistId)
    {
        string query = @"SELECT artist.name
                         FROM tidal_track_artist tta
                         join tidal_artist artist on artist.artistid = tta.artistid
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
}