using Dapper;
using MiniMediaScanner.Models.Spotify;
using Npgsql;
using SpotifyAPI.Web;

namespace MiniMediaScanner.Repositories;

public class SpotifyRepository
{
    private readonly string _connectionString;
    
    public SpotifyRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<List<string>> GetSpotifyArtistIdsByNameAsync(string artist)
    {
        string query = @"SELECT Id FROM spotify_artist where lower(name) = lower(@artist)";
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        return (await conn
            .QueryAsync<string>(query, new
            {
                artist
            }))
            .ToList();
    }
    
    public async Task<List<string>> GetAllSpotifyArtistIdsAsync()
    {
        string query = @"SELECT Id FROM spotify_artist order by name asc";
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        return (await conn
            .QueryAsync<string>(query))
            .ToList();
    }

    public async Task<List<SpotifyTrackModel>> GetTrackByArtistIdAsync(string artistId, string albumName, string trackName)
    {
        string query = @"SET LOCAL pg_trgm.similarity_threshold = 0.8;
                         select distinct on (track.TrackId)
                             track.name As TrackName,
                             track.TrackId,
                             track.AlbumId,
                             track.DiscNumber,
                             (track.durationms / 1000.0) * interval '1 second' as Duration,
                             track.Explicit,
                             track.Href as TrackHref,
                             track.TrackNumber,
                             track.Uri,
                             album.AlbumGroup,
                             album.AlbumType,
                             album.ReleaseDate,
                             album.TotalTracks,
                             album.Label,
                             album.name as AlbumName,
                             artist.Href as ArtistHref,
                             artist.Genres,
                             artist.name as ArtistName,
                             artist.id as ArtistId,
                             ste.value as Isrc,
                             sbe.value as Upc
                         from spotify_track track
                         join spotify_album album on album.albumid = track.albumid and album.albumgroup in ('album', 'single') and album.albumtype in ('album', 'single')
                         join spotify_track_artist track_artist on track_artist.trackid = track.trackid
                         join spotify_album_artist album_artist on album_artist.albumid = album.albumid 
                         join spotify_artist artist on artist.id = track_artist.artistid
                         left join spotify_track_externalid ste on ste.trackid = track.trackid and ste.name = 'isrc'
                         left join spotify_album_externalid sbe on sbe.albumid = album.albumid and sbe.name = 'upc'
                         where artist.id = @artistId
	                         and (length(@albumName) = 0 OR lower(album.name) % lower(@albumName))
	                         and (length(@trackName) = 0 OR lower(track.name) % lower(@trackName))";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        var transaction = await conn.BeginTransactionAsync();
        var tracks = new List<SpotifyTrackModel>();

        try
        {
            tracks = (await conn
                    .QueryAsync<SpotifyTrackModel>(query, new
                    {
                        artistId,
                        albumName,
                        trackName
                    }, commandTimeout: 60,
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

    public async Task<List<SpotifyExternalValue>> GetTrackExternalValuesAsync(string trackId)
    {
        string query = @"select
	                         trackid as Id,
	                         Name,
	                         Value
                         from spotify_track_externalid externalvalue
                         where externalvalue.trackid = @trackId";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        return (await conn
            .QueryAsync<SpotifyExternalValue>(query,
                param: new
                {
                    trackId
                }))
            .ToList();
    }
    public async Task<List<SpotifyExternalValue>> GetAlbumExternalValuesAsync(string albumId)
    {
        string query = @"select
	                         albumid as Id,
	                         Name,
	                         Value
                         from spotify_album_externalid externalvalue
                         where externalvalue.albumid = @albumId";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        return (await conn
            .QueryAsync<SpotifyExternalValue>(query,
                param: new
                {
                    albumId
                }))
            .ToList();
    }
    public async Task<List<string>> GetTrackArtistsAsync(string trackId)
    {
        string query = @"SELECT artist.name
                         FROM spotify_track_artist sta
                         join spotify_artist artist on artist.id = sta.artistid
                         where sta.trackid = @trackId";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        return (await conn
            .QueryAsync<string>(query,
                param: new
                {
                    trackId
                }))
            .ToList();
    }
    
    public async Task<string?> GetHighestQualityAlbumCoverUrlAsync(string artistId, string albumName)
    {
        string query = @"SELECT sai.url
                         FROM spotify_album_image sai
                         join spotify_album album on album.albumid = sai.albumid and lower(album.name) = lower(@albumName)
                         join spotify_album_artist saa on saa.artistid = @artistId and saa.albumid = sai.albumid
                         order by sai.height desc, sai.width desc
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
    
    public async Task<string?> GetHighestQualityArtistCoverUrlAsync(string artistId)
    {
        string query = @"SELECT url
                         FROM spotify_artist_image sai
                         where sai.artistid = @artistId
                         order by height desc, width desc
                         limit 1";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn
            .QueryFirstOrDefaultAsync<string>(query,
                param: new
                {
                    artistId
                });
    }
    
    public async Task<bool> SpotifyAlbumIdExistsAsync(string albumId)
    {
        string query = @"SELECT 1
                         FROM spotify_album album
                         where album.albumid = @albumId
                         limit 1";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn
            .ExecuteScalarAsync<int?>(query,
                param: new
                {
                    albumId
                })) == 1;
    }
    
    public async Task<int> GetAlbumTrackCountAsync(string albumId)
    {
        string query = @"SELECT count(track.trackid)
                         FROM spotify_album album
                         join spotify_track track on track.albumid = album.albumid
                         where album.albumid = @albumId
                         limit 1";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn
            .ExecuteScalarAsync<int>(query,
                param: new
                {
                    albumId
                });
    }
}