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
    
    public async Task<List<int>> GetAllTidalArtistIdsAsync()
    {
        string query = @"SELECT artistid 
                         FROM tidal_artist 
                         order by popularity desc";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        return (await conn
                .QueryAsync<int>(query))
            .ToList();
    }
    
    public async Task<List<TidalTrackModel>> GetTrackByArtistIdAsync(int artistId, string albumName, string trackName)
    {
        string query = @"SET LOCAL pg_trgm.similarity_threshold = 0.8;
                         select distinct on (track.isrc, album.barcodeid, album.title, artist.artistid)
                             track.title As TrackName,
                             track.version As TrackVersion,
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
                         join tidal_artist artist on artist.artistid = track_artist.artistid
                         left join tidal_track_external_link trackExtLink on trackExtLink.trackid = track.trackid and trackExtLink.meta_type = 'TIDAL_SHARING'
                         left join tidal_album_external_link albumExtLink on albumExtLink.albumid = album.albumid and albumExtLink.meta_type = 'TIDAL_SHARING'
                         left join tidal_artist_external_link artistExtLink on artistExtLink.artistid = artist.artistid and artistExtLink.meta_type = 'TIDAL_SHARING'
                         where artist.artistid = @artistId
                             and (length(@albumName) = 0 OR lower(album.title) % lower(@albumName))
	                         and (length(@trackName) = 0 OR lower(track.title) % lower(@trackName))
	                         and (length(track.availability) > 0 or length(track.mediatags) > 0)";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        var transaction = await conn.BeginTransactionAsync();
        var tracks = new List<TidalTrackModel>();

        try
        {
            tracks = (await conn
                                 .QueryAsync<TidalTrackModel>(query,
                                     param: new
                                     {
                                         artistId,
                                         albumName,
                                         trackName
                                     },
                                     transaction: transaction))
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

        return tracks;
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
    
    public async Task<string?> GetHighestQualityArtistCoverUrlAsync(int artistId)
    {
        string query = @"SELECT tail.href
                         FROM tidal_artist_image_link tail
                         where tail.artistid = @artistId
                         order by tail.meta_width desc, tail.meta_height desc
                         limit 1";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn
            .QueryFirstOrDefaultAsync<string>(query,
                param: new
                {
                    artistId
                });
    }
    
    public async Task<string?> GetHighestQualityAlbumCoverUrlAsync(int artistId, string albumName)
    {
        string query = @"SELECT tail.href
                         FROM tidal_album_image_link tail
                             
                         join tidal_album album on 
                             album.albumid = tail.albumid 
                             and lower(album.title) = lower(@albumName) 
                             and album.artistid = @artistId
                         order by tail.meta_width desc, tail.meta_height desc
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
    public async Task<List<int>> GetNonpulledTidalArtistIdsAsync()
    {
        string query = @"SELECT artistid 
                         FROM tidal_artist 
                         where lastsynctime < '2020-01-01'
                         order by popularity desc";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn
                .QueryAsync<int>(query))
            .ToList();
    }
}