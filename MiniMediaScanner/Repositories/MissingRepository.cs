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
    
    public List<MissingMusicBrainzRecordModel> GetMusicBrainzRecords(string artistName)
    {
        string query = @"select lower(re.title) as AlbumTitle, 
                                lower(ar.name) as ArtistName, 
                                lower(track.title) as TrackTitle, 
                                lower(re.status) as Status, 
                                track.recordingid
                         FROM musicbrainzartist ar
                         JOIN musicbrainzrelease re ON re.musicbrainzartistid = ar.musicbrainzartistid
                                                    --AND lower(re.country) = lower(ar.country)
                         AND (lower(re.status) = 'official' OR LENGTH(re.status) = 0)
                         JOIN musicbrainzreleasetrack track ON track.musicbrainzremotereleaseid = re.musicbrainzremotereleaseid
                         where lower(ar.name) = lower(@artistName)
                         	and track.title not like '%(%'";
        
        using var conn = new NpgsqlConnection(_connectionString);
        
        return conn
            .Query<MissingMusicBrainzRecordModel>(query, new
            {
                artistName
            }, commandTimeout: 60)
            .ToList();
    }
    public List<string> GetAssociatedArtists(string artistName)
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
        
        using var conn = new NpgsqlConnection(_connectionString);
        
        return conn
            .Query<string>(query, new
            {
                artistName
            }, commandTimeout: 60)
            .ToList();
    }
    public List<MetadataModel> GetMetadataByArtist(string artistName)
    {
	    string query = @$"SELECT m.MetadataId, 
						        m.Path, 
						        m.Title, 
						        m.AlbumId,
						        artist.name AS ArtistName,
						        album.title AS AlbumName,
						        m.MusicBrainzArtistId
						 FROM metadata m
						 JOIN albums album ON album.albumid = m.albumid
						 JOIN artists artist ON artist.artistid = album.artistid
						 where m.tag_alljsontags @> '{{""artist"": ""{artistName}""}}'
						 or m.tag_alljsontags @> '{{""AlbumArtist"": ""{artistName}""}}'
						 or m.tag_alljsontags->>'ARTISTS' ilike '%{artistName}%'";
        
	    using var conn = new NpgsqlConnection(_connectionString);
        
	    return conn
		    .Query<MetadataModel>(query, new
		    {
			    artistName
		    }, commandTimeout: 60)
		    .ToList();
    }
	
	
    public List<string> GetMissingTracksByArtistMusicBrainz(string artistName)
    {
        string query = @"WITH unique_tracks AS (
                         SELECT *
                         FROM (
                             select lower(re.title) as album_title, lower(ar.name) as artist_name, lower(track.title) as track_title, lower(re.status) as status,
                                    ROW_NUMBER() OVER (
                                        PARTITION BY lower(track.title), lower(re.title), lower(ar.name), lower(track.title)
                                    ) AS rn
                                    
                               FROM musicbrainzartist ar
                                 JOIN musicbrainzrelease re 
                                     ON re.musicbrainzartistid = ar.musicbrainzartistid
                                     --AND lower(re.country) = lower(ar.country)
                                      AND (lower(re.status) = 'official' OR LENGTH(re.status) = 0)
                                 JOIN musicbrainzreleasetrack track 
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

        using var conn = new NpgsqlConnection(_connectionString);
        
        return conn
            .Query<string>(query, new
            {
                artistName
            }, commandTimeout: 60)
            .ToList();
    }
	
	public List<string> GetMissingTracksByArtistMusicBrainz2(string artistName)
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
						    FROM musicbrainzreleasetrack mrt
						    JOIN musicbrainzrelease mr ON mrt.musicbrainzremotereleaseid = mr.musicbrainzremotereleaseid
						    JOIN musicbrainzartist ma ON mr.musicbrainzartistid = ma.musicbrainzartistid
						    where lower(ma.name) = lower(@artistName)
						)
						SELECT distinct mb.artist_name || ' - ' || mb.album_name || ' - ' || mb.track_name
						FROM MusicBrainzData mb
						LEFT JOIN MusicLibrary ml 
						    ON (lower(mb.album_name) = lower(ml.album_name)
						    and similarity(mb.track_name, ml.track_name) >= 0.5)
						    or (similarity(mb.track_name, ml.track_name) >= 0.5)
							
						WHERE ml.track_name IS NULL";

        using var conn = new NpgsqlConnection(_connectionString);
        
        return conn
            .Query<string>(query, new
            {
                artistName
            }, commandTimeout: 60)
            .ToList();
    }
	
    public List<string> GetMissingTracksByArtistSpotify2(string artistName)
    {
	    string query = @"WITH MusicLibrary AS (
						    SELECT 
						        a.artistid, 
						        a.name AS artist_name, 
						        al.albumid, 
						        al.title AS album_name, 
						        m.title AS track_name
						    FROM metadata m
						    JOIN albums al ON m.albumid = al.albumid
						    JOIN artists a ON al.artistid = a.artistid
					    join metadata_tag tag on tag.metadataid = m.metadataid
    											 and ((tag.name in ('ALBUM ARTIST', 'ALBUMARTIST', 'ALBUM_ARTIST', 'artist', 'album_artist', 'AlbumArtist', 'Artists') and tag.value ilike '%' || @artistName || '%')
    											    or (tag.name in ('ARTISTS', 'ALBUMARTISTS', 'Artists', 'artists', 'album_artists', 'albumartists') and tag.value ilike '%' || @artistName || '%'))
						),
						MusicBrainzData AS (
							select artist.id as artist_id, 
							        artist.name AS artist_name, 
							        album.name AS album_name, 
							        track.name AS track_name
							from spotify_track track
							 join spotify_album album on album.albumid = track.albumid
							 join spotify_track_artist track_artist on track_artist.trackid = track.trackid
							 join spotify_album_artist album_artist on album_artist.albumid = album.albumid 
							 join spotify_artist artist on artist.id = track_artist.artistid or 
														   artist.id = album_artist.artistid
						    where lower(artist.name) = lower(@artistName)
						)
						SELECT distinct mb.artist_name || ' - ' || mb.album_name || ' - ' || mb.track_name
						FROM MusicBrainzData mb
						LEFT JOIN MusicLibrary ml 
						    ON (lower(mb.album_name) = lower(ml.album_name)
						    and similarity(lower(mb.track_name), lower(ml.track_name)) >= 0.5)
						    or (similarity(lower(mb.track_name), lower(ml.track_name)) >= 0.5)
						WHERE ml.track_name IS null ";

	    using var conn = new NpgsqlConnection(_connectionString);
        
	    return conn
		    .Query<string>(query, new
		    {
			    artistName
		    }, commandTimeout: 60)
		    .ToList();
    }
}