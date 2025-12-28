using Dapper;
using Npgsql;

namespace MiniMediaScanner.Repositories;

public class MatchRepository
{
    private readonly string _connectionString;
    public MatchRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<string?> GetBestSpotifyMatchAsync(Guid artistId, string artistName)
    {
        string query = @"WITH MusicLibrary AS (
                             SELECT distinct
                                 a.artistid, 
                                 a.name AS artist_name, 
                                 al.albumid, 
                                 al.title AS album_name
                             FROM metadata m
                             JOIN albums al ON m.albumid = al.albumid
                             JOIN artists a ON al.artistid = a.artistid
                             WHERE a.artistid = @artistId
                         ),
                         SpotifyData AS (
                             select distinct
                                 artist.id AS artist_id, 
                                 artist.name AS artist_name, 
                                 album.name AS album_name
                             from spotify_artist artist
                             JOIN spotify_album album ON album.artistid = artist.id
                             WHERE lower(artist.name) = lower(@artistName)
                         )
                         SELECT 
                             sd.artist_id,
                             sd.artist_name,
                             ROUND(100.0 * COUNT(DISTINCT ml.album_name) / NULLIF((SELECT COUNT(DISTINCT album_name) FROM MusicLibrary ml), 0), 2) AS match_percentage
                         FROM SpotifyData sd
                         LEFT JOIN MusicLibrary ml 
                             ON lower(sd.album_name) = lower(ml.album_name)
                         GROUP BY sd.artist_id, sd.artist_name
                         ORDER BY match_percentage desc
                         limit 1";
        
        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn
            .QueryFirstOrDefaultAsync<string>(query, param: new
            {
                artistId,
                artistName
            });
    }
    
    public async Task<int?> GetBestTidalMatchAsync(Guid artistId, string artistName)
    {
        string query = @"WITH MusicLibrary AS (
                             SELECT distinct
                                 a.artistid, 
                                 a.name AS artist_name, 
                                 al.albumid, 
                                 al.title AS album_name
                             FROM metadata m
                             JOIN albums al ON m.albumid = al.albumid
                             JOIN artists a ON al.artistid = a.artistid
                             WHERE a.artistid = @artistId
                         ),
                         TidalData AS (
                             select distinct
                                 artist.artistid, 
                                 artist.name AS artist_name,
                                 album.title AS album_name
                             from tidal_artist artist
                             join tidal_album album on album.artistid = artist.artistid
                             join tidal_track track on track.albumid = album.albumid
                             WHERE lower(artist.name) = lower(@artistName)
                         )
                         SELECT 
                             td.artistid,
                             td.artist_name,
                             ROUND(100.0 * COUNT(DISTINCT ml.album_name) / NULLIF((SELECT COUNT(DISTINCT album_name) FROM MusicLibrary ml), 0), 2) AS match_percentage
                         FROM TidalData td
                         LEFT JOIN MusicLibrary ml 
                             ON lower(td.album_name) = lower(ml.album_name)
                         GROUP BY td.artistid, td.artist_name
                         ORDER BY match_percentage desc
                         limit 1";
        
        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn
            .QueryFirstOrDefaultAsync<int>(query, param: new
            {
                artistId,
                artistName
            });
    }
    
    
    public async Task<long?> GetBestDeezerMatchAsync(Guid artistId, string artistName)
    {
        string query = @"WITH MusicLibrary AS (
                             SELECT 
                                 a.artistid, 
                                 a.name AS artist_name, 
                                 al.albumid, 
                                 al.title AS album_name
                             FROM albums al
                             JOIN artists a ON a.artistid = al.artistid
                             where a.artistid = @artistId
                         ),
                         DeezerData AS (
                            SELECT 
                                artist.artistid AS artist_id, 
                                artist.name AS artist_name, 
                                album.title AS album_name
                            FROM deezer_artist artist
                            JOIN deezer_album album ON album.artistid = artist.artistid
                            left JOIN deezer_track_artist track_artist ON track_artist.artistid = artist.artistid
                            left JOIN deezer_album_artist album_artist ON album_artist.albumid = album.albumid 
                            WHERE lower(artist.name) = lower(@artistName)
                         )
                         SELECT 
                             dd.artist_id,
                             dd.artist_name,
                             ROUND(100.0 * COUNT(DISTINCT ml.album_name) / NULLIF((SELECT COUNT(DISTINCT album_name) FROM MusicLibrary ml), 0), 2) AS match_percentage
                         FROM DeezerData dd
                         LEFT JOIN MusicLibrary ml 
                             ON lower(dd.album_name) = lower(ml.album_name)
                         GROUP BY dd.artist_id, dd.artist_name
                         ORDER BY match_percentage desc
                         limit 1";
        
        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn
            .QueryFirstOrDefaultAsync<long>(query, param: new
            {
                artistId,
                artistName
            });
    }
    
    
    public async Task<Guid?> GetBestMusicBrainzMatchAsync(Guid artistId, string artistName)
    {
        string query = @"WITH MusicLibrary AS (
                             SELECT distinct
                                 a.artistid, 
                                 a.name AS artist_name, 
                                 al.albumid, 
                                 al.title AS album_name
                             FROM metadata m
                             JOIN albums al ON m.albumid = al.albumid
                             JOIN artists a ON al.artistid = a.artistid
                             WHERE a.artistid = @artistId
                         ),
                         MusicBrainzData AS (
						    SELECT distinct
						        ma.artistid, 
						        ma.name AS artist_name, 
						        mr.title AS album_name
						    FROM MusicBrainz_Release_Track mrt
						    JOIN MusicBrainz_Release mr ON mr.releaseid = mrt.releaseid
						    JOIN MusicBrainz_Artist ma ON ma.artistid = mr.artistid
						    WHERE lower(ma.name) = lower(@artistName)
                         )
                         SELECT 
                             mb.artistid,
                             mb.artist_name,
                             ROUND(100.0 * COUNT(DISTINCT ml.album_name) / NULLIF((SELECT COUNT(DISTINCT album_name) FROM MusicLibrary ml), 0), 2) AS match_percentage
                         FROM MusicBrainzData mb
                         LEFT JOIN MusicLibrary ml 
                             ON lower(mb.album_name) = lower(ml.album_name)
                         GROUP BY mb.artistid, mb.artist_name
                         ORDER BY match_percentage desc
                         limit 1";
        
        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn
            .QueryFirstOrDefaultAsync<Guid>(query, param: new
            {
                artistId,
                artistName
            });
    }
    
    public async Task<int?> GetBestDiscogsMatchAsync(Guid artistId, string artistName)
    {
        string query = @"WITH MusicLibrary AS (
                             SELECT distinct
                                 a.artistid, 
                                 a.name AS artist_name, 
                                 al.albumid, 
                                 al.title AS album_name
                             FROM metadata m
                             JOIN albums al ON m.albumid = al.albumid
                             JOIN artists a ON al.artistid = a.artistid
                             WHERE a.artistid = @artistId
                         ),
                         DiscogsData AS (
                             select distinct
                                 artist.artistid, 
                                 artist.name AS artist_name,
                                 album.title AS album_name
                             from discogs_artist artist
                             join discogs_release_artist dra on dra.artistid =  artist.artistid
                             join discogs_release album on album.releaseid = dra.releaseid
                             join discogs_release_track track on track.releaseid = album.releaseid
                             WHERE lower(artist.name) = lower(@artistName)
                         )
                         SELECT 
                             dd.artistid,
                             dd.artist_name,
                             ROUND(100.0 * COUNT(DISTINCT ml.album_name) / NULLIF((SELECT COUNT(DISTINCT album_name) FROM MusicLibrary ml), 0), 2) AS match_percentage
                         FROM DiscogsData dd
                         LEFT JOIN MusicLibrary ml 
                             ON lower(dd.album_name) = lower(ml.album_name)
                         GROUP BY dd.artistid, dd.artist_name
                         ORDER BY match_percentage desc
                         limit 1";
        
        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn
            .QueryFirstOrDefaultAsync<int>(query, param: new
            {
                artistId,
                artistName
            });
    }
    
}