using System.Text;
using Dapper;
using MiniMediaScanner.Models.Deezer;
using Npgsql;

namespace MiniMediaScanner.Repositories;

public class DeezerRepository
{
    public const int PagingSize = 1000;
    private readonly string _connectionString;
    public DeezerRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<List<string>> GetTrackArtistsAsync(long trackId, long orderByArtistId)
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
    
    public async Task<List<DeezerTrackDbModel>> GetTrackByArtistIdAsync(long artistId, string albumName, string trackName)
    {
        string query = @"SET LOCAL pg_trgm.similarity_threshold = 0.8;
                         select distinct on (track.isrc, album.upc, album.title, artist.artistid)
                             track.title As TrackName,
                             track.TrackId,
                             track.AlbumId,
                             track.disknumber AS DiscNumber,
                             track.duration * interval '1 sec' as Duration,
                             track.ExplicitLyrics,
                             'https://www.deezer.com/track/' || track.TrackId AS TrackHref,
                             track.TrackPosition,
                             track.isrc AS TrackISRC,
                             album.upc AS AlbumUPC,
                             album.ReleaseDate as AlbumReleaseDate,
                             album.nbtracks AS AlbumTotalTracks,
                             album.label as Label,
                             album.title as AlbumName,
                             'https://www.deezer.com/album/' || album.albumid as AlbumHref,
                             'https://www.deezer.com/artist/' || artist.artistid as ArtistHref,
                             artist.name as ArtistName,
                             artist.artistid as ArtistId
                         from deezer_track track
                         join deezer_album album on album.albumid = track.albumid
                         join deezer_track_artist track_artist on track_artist.trackid = track.trackid
                         join deezer_artist artist on artist.artistid = track_artist.artistid
                         where artist.artistid = @artistId
                             and (length(@albumName) = 0 OR lower(album.title) % lower(@albumName))
	                         and (length(@trackName) = 0 OR lower(track.title) % lower(@trackName))";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        var transaction = await conn.BeginTransactionAsync();
        var tracks = new List<DeezerTrackDbModel>();

        try
        {
            tracks = (await conn
                    .QueryAsync<DeezerTrackDbModel>(query,
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
    
    public async Task<List<int>> GetAllDeezerArtistIdsAsync(int offset)
    {
        string query = @"SELECT artistid FROM deezer_artist 
                         order by name asc 
                         offset @offset
                         limit @PagingSize";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn
                .QueryAsync<int>(query, param: new
                {
                    offset,
                    PagingSize
                }))
            .ToList();
    }
}