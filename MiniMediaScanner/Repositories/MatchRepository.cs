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
                             SELECT distinct
                                 artist.id AS artist_id, 
                                 artist.name AS artist_name, 
                                 album.name AS album_name
                             FROM spotify_track track
                             JOIN spotify_album album ON album.albumid = track.albumid
                             JOIN spotify_track_artist track_artist ON track_artist.trackid = track.trackid
                             JOIN spotify_album_artist album_artist ON album_artist.albumid = album.albumid 
                             JOIN spotify_artist artist ON artist.id = track_artist.artistid OR 
                                                            artist.id = album_artist.artistid
                             WHERE lower(artist.name) = lower(@artistName)
                         )
                         SELECT 
                             sd.artist_id,
                             sd.artist_name,
                             ROUND(100.0 * COUNT(DISTINCT sd.album_name) / NULLIF((SELECT COUNT(DISTINCT album_name) FROM MusicLibrary ml), 0), 2) AS match_percentage
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
                             ROUND(100.0 * COUNT(DISTINCT td.album_name) / NULLIF((SELECT COUNT(DISTINCT album_name) FROM MusicLibrary ml), 0), 2) AS match_percentage
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
                             ROUND(100.0 * COUNT(DISTINCT dd.album_name) / NULLIF((SELECT COUNT(DISTINCT album_name) FROM MusicLibrary ml), 0), 2) AS match_percentage
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
    
}