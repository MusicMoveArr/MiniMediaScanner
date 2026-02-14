using Dapper;
using MiniMediaScanner.Models.Tidal;
using Npgsql;

namespace MiniMediaScanner.Repositories;

public class UpdateTidalRepository : BaseUpdateRepository
{
    public UpdateTidalRepository(string connectionString)
        : base(connectionString)
    {
        
    }
    
    public async Task<DateTime?> GetArtistLastSyncTimeAsync(int artistId)
    {
        string query = @"SELECT lastsynctime FROM tidal_artist WHERE ArtistId = @id";

        return await base.Connection
            .ExecuteScalarAsync<DateTime>(query, new
            {
                id = artistId
            }, transaction: base.Transaction);
    }
    
    public async Task<DateTime?> SetArtistLastSyncTimeAsync(int artistId)
    {
        string query = @"UPDATE tidal_artist SET lastsynctime = @lastsynctime WHERE ArtistId = @id";

        return await base.Connection
            .ExecuteScalarAsync<DateTime>(query, new
            {
                id = artistId,
                lastsynctime = DateTime.Now
            }, transaction: base.Transaction);
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
                Popularity = EXCLUDED.Popularity";

        await base.Connection
            .ExecuteAsync(query, param: new
            {
                artistId,
                name,
                popularity,
                lastsynctime = new DateTime(2000, 1, 1)
            }, transaction: base.Transaction);
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

        await base.Connection
            .ExecuteAsync(query, param: new
            {
                artistId,
                href,
                metaWidth,
                metaHeight
            }, transaction: base.Transaction);
    }
    
    public async Task UpsertArtistExternalLinkAsync(int artistId, string href, string metaType)
    {
        string query = @"
            INSERT INTO tidal_artist_external_link (ArtistId, href, meta_type)
            VALUES (@artistId, @href, @metaType)
            ON CONFLICT (ArtistId, href, meta_type)
            DO NOTHING";

        await base.Connection
            .ExecuteAsync(query, param: new
            {
                artistId,
                href,
                metaType
            }, transaction: base.Transaction);
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

        await base.Connection
            .ExecuteAsync(query, param: new
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
            }, transaction: base.Transaction);
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

        await base.Connection
            .ExecuteAsync(query, param: new
            {
                albumId,
                href,
                metaWidth,
                metaHeight
            }, transaction: base.Transaction);
    }
    
    public async Task UpsertAlbumExternalLinkAsync(int albumId, string href, string metaType)
    {
        string query = @"
            INSERT INTO tidal_album_external_link (AlbumId, href, meta_type)
            VALUES (@albumId, @href, @metaType)
            ON CONFLICT (albumId, href, meta_type)
            DO NOTHING";

        await base.Connection
            .ExecuteAsync(query, param: new
            {
                albumId,
                href,
                metaType
            }, transaction: base.Transaction);
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
        int trackNumber,
        string version)
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
                                  TrackNumber,
                                  Version)
            VALUES (@trackId, @albumId, @title, @isrc, 
                    @duration, @copyright, @_explicit, 
                    @popularity, @availability, @mediaTags, 
                    @volumeNumber, @trackNumber, @version)
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
                TrackNumber = EXCLUDED.TrackNumber,
                Version = EXCLUDED.Version";

        await base.Connection
            .ExecuteAsync(query, param: new
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
                trackNumber,
                version
            }, transaction: base.Transaction);
    }
    
    public async Task UpsertTrackExternalLinkAsync(int trackId, string href, string metaType)
    {
        string query = @"
            INSERT INTO tidal_track_external_link (TrackId, href, meta_type)
            VALUES (@trackId, @href, @metaType)
            ON CONFLICT (TrackId, href, meta_type)
            DO NOTHING";

        await base.Connection
            .ExecuteAsync(query, param: new
            {
                trackId,
                href,
                metaType
            }, transaction: base.Transaction);
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

        await base.Connection
            .ExecuteAsync(query, param: new
            {
                trackId,
                href,
                metaWidth,
                metaHeight
            }, transaction: base.Transaction);
    }
    
    public async Task<int> GetTidalAlbumTrackCountAsync(int albumId, int artistId)
    {
        string query = @"SELECT count(track.trackid)
                         FROM tidal_album album
                         join tidal_track track on track.albumid = album.albumid
                         where album.albumid = @albumId and album.artistId = @artistId
                         limit 1";

        return await base.Connection
            .ExecuteScalarAsync<int>(query,
                param: new
                {
                    albumId,
                    artistId
                }, transaction: base.Transaction);
    }
    
    public async Task UpsertTrackArtistIdAsync(int trackId, int artistId)
    {
        string query = @"
            INSERT INTO tidal_track_artist (TrackId, ArtistId)
            VALUES (@trackId, @artistId)
            ON CONFLICT (TrackId, ArtistId)
            DO NOTHING";

        await base.Connection
            .ExecuteAsync(query, param: new
            {
                trackId,
                artistId
            }, transaction: base.Transaction);
    }
    
    public async Task UpsertProviderAsync(int providerId, string name)
    {
        string query = @"
            INSERT INTO tidal_provider (ProviderId, Name)
            VALUES (@providerId, @name)
            ON CONFLICT (ProviderId)
            DO NOTHING";

        await base.Connection
            .ExecuteAsync(query, param: new
            {
                providerId,
                name
            }, transaction: base.Transaction);
    }
    
    public async Task UpsertTrackProviderAsync(int trackId, int providerId)
    {
        string query = @"
            INSERT INTO tidal_track_provider (TrackId, ProviderId)
            VALUES (@trackId, @providerId)
            ON CONFLICT (TrackId, ProviderId)
            DO NOTHING";

        await base.Connection
            .ExecuteAsync(query, param: new
            {
                trackId,
                providerId
            }, transaction: base.Transaction);
    }
    
    public async Task UpsertSimilarTrackAsync(int trackId, int similarTrackId, string similarIsrc)
    {
        string query = @"
            INSERT INTO tidal_track_similar (TrackId, SimilarTrackId, SimilarISRC)
            VALUES (@trackId, @similarTrackId, @similarIsrc)
            ON CONFLICT (TrackId, SimilarTrackId)
            DO NOTHING";

        await base.Connection
            .ExecuteAsync(query, param: new
            {
                trackId,
                similarTrackId,
                similarIsrc
            }, transaction: base.Transaction);
    }
    
    public async Task UpsertSimilarArtistAsync(int artistId, int similarArtistId)
    {
        string query = @"
            INSERT INTO tidal_artist_similar (ArtistId, SimilarArtistId)
            VALUES (@artistId, @similarArtistId)
            ON CONFLICT (ArtistId, SimilarArtistId)
            DO NOTHING";

        await base.Connection
            .ExecuteAsync(query, param: new
            {
                artistId,
                similarArtistId
            }, transaction: base.Transaction);
    }
    
    public async Task<bool> HasSimilarTrackRecordsAsync(int trackId)
    {
        string query = @"
            select 1 from tidal_track_similar 
            where trackId = @trackId";

        return (await base.Connection
            .ExecuteScalarAsync<int>(query, param: new
            {
                trackId
            }, transaction: base.Transaction)) == 1;
    }
    
    public async Task<bool> HasSimilarArtistRecordsAsync(int artistId)
    {
        string query = @"
            select 1 from tidal_artist_similar 
            where artistId = @artistId";

        return (await base.Connection
            .ExecuteScalarAsync<int>(query, param: new
            {
                artistId
            }, transaction: base.Transaction)) == 1;
    }
    
    public async Task<List<int>> GetMissingSimilarTrackIdsByArtistIdAsync(int artistId)
    {
        string query = @"
                    select track.TrackId
                    from tidal_artist artist
                    join tidal_album album on album.artistid = artist.artistid 
                    join tidal_track track on track.albumid = album.albumid 
                    left join tidal_track_similar sim on sim.trackid = track.trackid 
                    where artist.artistid = @artistId and sim.trackid is null";

        return (await base.Connection
            .QueryAsync<int>(query, param: new
            {
                artistId
            }, transaction: base.Transaction))
            .ToList();
    }
    
    public async Task<List<int>> GetMissingSimilarArtistIdsByArtistIdAsync(int artistId)
    {
        string query = @"
                    select artist.artistid
                    from tidal_artist artist
                    left join tidal_artist_similar sam on sam.artistid = artist.artistid 
                    where artist.artistid = @artistId and sam.artistid is null";

        return (await base.Connection
                .QueryAsync<int>(query, param: new
                {
                    artistId
                }, transaction: base.Transaction))
            .ToList();
    }
}