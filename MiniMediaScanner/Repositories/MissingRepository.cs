using Dapper;
using MiniMediaScanner.Models;
using Npgsql;

namespace MiniMediaScanner.Repositories;

public class MissingRepository
{
    private readonly string _connectionString;
    public MissingRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<List<MissingMusicBrainzRecordModel>> GetMusicBrainzRecordsAsync(string artistName)
    {
        string query = @"select lower(re.title) as AlbumTitle, 
                                lower(ar.name) as ArtistName, 
                                lower(track.title) as TrackTitle, 
                                lower(re.status) as Status, 
                                track.recordingid
                         FROM MusicBrainz_Artist ar
                         JOIN MusicBrainz_Release re ON re.musicbrainzartistid = ar.musicbrainzartistid
                                                    --AND lower(re.country) = lower(ar.country)
                         AND (lower(re.status) = 'official' OR LENGTH(re.status) = 0)
                         JOIN MusicBrainz_Release_Track track ON track.musicbrainzremotereleaseid = re.musicbrainzremotereleaseid
                         where lower(ar.name) = lower(@artistName)
                         	and track.title not like '%(%'";
        
        await using var conn = new NpgsqlConnection(_connectionString);
        
        return (await conn
            .QueryAsync<MissingMusicBrainzRecordModel>(query, new
            {
                artistName
            }, commandTimeout: 60))
            .ToList();
    }
    public async Task<List<string>> GetAssociatedArtists(string artistName)
    {
        string query = @"WITH RECURSIVE artist_names AS (
						    SELECT DISTINCT unnest(string_to_array(
						        replace(replace(
								            COALESCE(tag_alljsontags->>'Artists', tag_alljsontags->>'ARTISTS'), 
								            '&', ';'), 
								            '/', ';'),
								        ';'
								    )) AS artist
							from artists artist
							join albums album on album.artistid = artist.artistid
							join metadata m on m.albumid = album.albumid
							where lower(artist.""name"") = lower(@artistName)
						)
						SELECT distinct lower(artist)
						FROM artist_names
						WHERE artist IS NOT NULL AND artist <> ''";
        
        await using var conn = new NpgsqlConnection(_connectionString);
        
        return (await conn
            .QueryAsync<string>(query, new
            {
                artistName
            }, commandTimeout: 60))
            .ToList();
    }
    public async Task<List<MetadataModel>> GetMetadataByArtistAsync(string artistName)
    {
	    string query = @$"SELECT m.MetadataId, 
						        m.Path, 
						        m.Title, 
						        m.AlbumId,
						        artist.name AS ArtistName,
						        album.title AS AlbumName,
						        m.MusicBrainzArtistId,
								artist.artistid as ArtistId
						 FROM metadata m
						 JOIN albums album ON album.albumid = m.albumid
						 JOIN artists artist ON artist.artistid = album.artistid
						 where m.tag_alljsontags @> '{{""artist"": ""{artistName}""}}'
						 or m.tag_alljsontags @> '{{""AlbumArtist"": ""{artistName}""}}'
						 or m.tag_alljsontags->>'ARTISTS' ilike '%{artistName}%'";
        
	    await using var conn = new NpgsqlConnection(_connectionString);
        
	    return (await conn
		    .QueryAsync<MetadataModel>(query, new
		    {
			    artistName
		    }, commandTimeout: 60))
		    .ToList();
    }
	
	
    public async Task<List<string>> GetMissingTracksByArtistMusicBrainzAsync(string artistName)
    {
        string query = @"WITH unique_tracks AS (
                         SELECT *
                         FROM (
                             select lower(re.title) as album_title, lower(ar.name) as artist_name, lower(track.title) as track_title, lower(re.status) as status,
                                    ROW_NUMBER() OVER (
                                        PARTITION BY lower(track.title), lower(re.title), lower(ar.name), lower(track.title)
                                    ) AS rn
                                    
                               FROM MusicBrainz_Artist ar
                                 JOIN MusicBrainz_Release re 
                                     ON re.musicbrainzartistid = ar.musicbrainzartistid
                                     --AND lower(re.country) = lower(ar.country)
                                      AND (lower(re.status) = 'official' OR LENGTH(re.status) = 0)
                                 JOIN MusicBrainz_Release_Track track 
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
	                     or (m.path ilike '%/' || ut.album_title || '/%' --check album by path
	                        and m.path ilike '%/' || ut.artist_name || '/%' --check album by artist
	                         and lower(m.title) = ut.track_title)
	                     or (m.path ilike '%/' || ut.artist_name || '/%' --check by just the arist path
	                         and lower(m.title) = ut.track_title)
	                     or (lower(m.title) = ut.track_title and m.path ilike '%/' || ut.artist_name || '/%')
 
                     where ut.artist_name = lower(@artistName)
                     and m.metadataid is null";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        return (await conn
            .QueryAsync<string>(query, new
            {
                artistName
            }, commandTimeout: 60))
            .ToList();
    }
	
	public async Task<List<MissingTrackModel>> GetMissingTracksByArtistMusicBrainz2Async(string artistName)
    {
        string query = @"WITH MusicLibrary AS (
						    SELECT 
						        a.artistid, 
						        a.name AS artist_name, 
						        al.albumid, 
						        al.title AS album_name, 
						        m.metadataid, 
						        m.title AS track_name, 
						        m.musicbrainztrackid ,
						        m.path
						    FROM metadata m
						    JOIN albums al ON m.albumid = al.albumid
						    JOIN artists a ON al.artistid = a.artistid
						    where lower(a.name) = lower(@artistName)
						),
						MusicBrainzData AS (
						    SELECT 
						        ma.musicbrainzartistid, 
						        ma.name AS artist_name, 
						        mr.musicbrainzreleaseid, 
						        mr.title AS album_name, 
						        mrt.musicbrainzreleasetrackid, 
						        mrt.title AS track_name 
						    FROM MusicBrainz_Release_Track mrt
						    JOIN MusicBrainz_Release mr ON mrt.musicbrainzremotereleaseid = mr.musicbrainzremotereleaseid
						    JOIN MusicBrainz_Artist ma ON mr.musicbrainzartistid = ma.musicbrainzartistid
						    where lower(ma.name) = lower(@artistName)
						)
						SELECT distinct mb.artist_name AS Artist, mb.album_name AS Album, mb.track_name AS Track
						FROM MusicBrainzData mb
						LEFT JOIN MusicLibrary ml 
						    ON (lower(mb.album_name) = lower(ml.album_name)
						    and similarity(mb.track_name, ml.track_name) >= 0.5)
						    or (similarity(mb.track_name, ml.track_name) >= 0.5)
							
						WHERE ml.track_name IS NULL";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        return (await conn
            .QueryAsync<MissingTrackModel>(query, new
            {
                artistName
            }, commandTimeout: 60))
            .ToList();
    }
	
    public async Task<List<MissingTrackModel>> GetMissingTracksByArtistSpotify2Async(string spotifyArtistId, string artistName)
    {
	    string query = @"WITH SpotifyData AS (
						 	select artist.id as artist_id, 
						 	       artist.name AS artist_name, 
						 	       album.name AS album_name, 
						 	       track.name AS track_name,
								   'https://open.spotify.com/artist/' || artist.id AS ArtistUrl,
								   'https://open.spotify.com/album/' || album.albumid AS AlbumUrl,
								   'https://open.spotify.com/track/' || track.trackid AS TrackUrl
						 	from spotify_track track
						 	 join spotify_album album on album.albumid = track.albumid and album.albumgroup in ('album', 'single') and album.albumtype in ('album', 'single')
						 	 join spotify_track_artist track_artist on track_artist.trackid = track.trackid
						 	 join spotify_album_artist album_artist on album_artist.albumid = album.albumid 
						 	 join spotify_artist artist on artist.id = track_artist.artistid or 
						 								   artist.id = album_artist.artistid
						     where artist.id = @spotifyArtistId
						 ),
						 MusicLibrary as (
						 	select distinct 
						 		m.title as track_name,
						 		album.title as album_name
						 	from metadata m
						 	join albums album on album.albumid = m.albumid
						 	join artists artist on artist.artistid = album.artistid and lower(artist.name) = lower(@artistName)
						 )
 
						 select distinct 
									sd.artist_name AS Artist, 
									sd.album_name AS Album, 
									sd.track_name AS Track,
									sd.ArtistUrl,
									sd.TrackUrl,
									sd.AlbumUrl
						 FROM SpotifyData sd
						 left join MusicLibrary ml on similarity(sd.album_name, ml.album_name) >= 0.9 
						 							and similarity(sd.track_name, ml.track_name) >= 0.9 
						 where ml.track_name is null";

	    await using var conn = new NpgsqlConnection(_connectionString);
        
	    return (await conn
		    .QueryAsync<MissingTrackModel>(query, new
		    {
			    spotifyArtistId,
			    artistName
		    }, commandTimeout: 60))
		    .ToList();
    }
    
    public async Task<List<MissingTrackModel>> GetMissingTracksByArtistTidalAsync(int tidalArtistId, string artistName)
    {
	    string query = @"WITH TidalData AS (
						 	select artist.artistid, 
						 		artist.name AS artist_name,
						 		album.title AS album_name,
								case when length(track.version) > 0 then 
									track.title || ' (' || track.version || ')'
								else
									track.title
								end AS track_name,
						 		taael.href AS ArtistUrl,
						 		ttel.href AS TrackUrl,
						 		tael.href AS AlbumUrl
						 	from tidal_artist artist
						 	join tidal_album album on album.artistid = artist.artistid and length(album.Availability) > 0
						 	join tidal_track track on track.albumid = album.albumid and length(track.Availability) > 0
							left join tidal_album_external_link tael on tael.albumid = album.albumid and tael.meta_type = 'TIDAL_SHARING'
							left join tidal_track_external_link ttel on ttel.trackid = track.trackid and ttel.meta_type = 'TIDAL_SHARING'
							left join tidal_artist_external_link taael on taael.artistid = artist.artistid and taael.meta_type = 'TIDAL_SHARING'
						 	where artist.artistid = @tidalArtistId

						 ),
						 MusicLibrary as (
						 	select distinct 
						 		m.title as track_name,
						 		album.title as album_name
						 	from metadata m
						 	join albums album on album.albumid = m.albumid
						 	join artists artist on artist.artistid = album.artistid and lower(artist.name) = lower(@artistName)
						 )

						 select distinct 
									td.artist_name AS Artist, 
									td.album_name AS Album, 
									td.track_name AS Track,
									td.ArtistUrl,
									td.TrackUrl,
									td.AlbumUrl
						 FROM TidalData td
						 left join MusicLibrary ml on similarity(td.album_name, ml.album_name) >= 0.9 
						 							and similarity(td.track_name, ml.track_name) >= 0.9 
						 where ml.track_name is null";

	    await using var conn = new NpgsqlConnection(_connectionString);
        
	    return (await conn
			    .QueryAsync<MissingTrackModel>(query, new
			    {
				    tidalArtistId,
				    artistName
			    }, commandTimeout: 300))
		    .ToList();
    }
    
    public async Task<List<MissingTrackModel>> GetMissingTracksByArtistDeezerAsync(long deezerArtistId, string artistName)
    {
	    string query = @"WITH DeezerData AS (
 							select artist.artistid, 
 								artist.name AS artist_name,
 								album.title AS album_name,
								track.title AS track_name,
 								'https://www.deezer.com/artist/' || artist.artistid AS ArtistUrl,
 								'https://www.deezer.com/track/' || track.TrackId AS TrackUrl,
 								'https://www.deezer.com/album/' || album.albumid AS AlbumUrl
 							from deezer_artist artist
 							join deezer_album album on album.artistid = artist.artistid
 							join deezer_track track on track.albumid = album.albumid
						 	where artist.artistid = @deezerArtistId
						 ),
						 MusicLibrary as (
						 	select distinct 
						 		m.title as track_name,
						 		album.title as album_name
						 	from metadata m
						 	join albums album on album.albumid = m.albumid
						 	join artists artist on artist.artistid = album.artistid and lower(artist.name) = lower(@artistName)
						 )
						 select distinct 
									dd.artist_name AS Artist, 
									dd.album_name AS Album, 
									dd.track_name AS Track,
									dd.ArtistUrl,
									dd.TrackUrl,
									dd.AlbumUrl
						 FROM DeezerData dd
						 left join MusicLibrary ml on similarity(dd.album_name, ml.album_name) >= 0.9 
						 							and similarity(dd.track_name, ml.track_name) >= 0.9 
						 where ml.track_name is null";

	    await using var conn = new NpgsqlConnection(_connectionString);
        
	    return (await conn
			    .QueryAsync<MissingTrackModel>(query, new
			    {
				    deezerArtistId,
				    artistName
			    }, commandTimeout: 300))
		    .ToList();
    }

    public async Task<bool> TrackExistsAtAssociatedArtist(string artistName, string albumName, string trackName)
    {
	    string query = @"SELECT 1
						 FROM metadata m
						 JOIN albums al ON m.albumid = al.albumid
						 JOIN artists a ON al.artistid = a.artistid
						 JOIN LATERAL (
						     SELECT jsonb_object_keys(m.tag_alljsontags) AS key
						 ) subquery ON LOWER(subquery.key) LIKE '%artists%'
						 WHERE LOWER(al.title) = lower(@albumName)
						 AND similarity(LOWER(m.title), lower(@trackName)) >= 0.9 
						 AND LOWER(m.tag_alljsontags->>subquery.key) ILIKE '%' || lower(@artistName) || '%'
						 LIMIT 1";
	    
	    await using var conn = new NpgsqlConnection(_connectionString);

	    return await conn
		    .ExecuteScalarAsync<bool>(query, new
		    {
			    artistName,
			    albumName,
			    trackName
		    }, commandTimeout: 60);
    }
}