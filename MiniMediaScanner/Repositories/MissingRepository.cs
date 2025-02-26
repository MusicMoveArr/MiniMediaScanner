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
                         JOIN musicbrainzrelease re ON re.musicbrainzartistid = CAST(ar.musicbrainzartistid AS TEXT)
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
                                     ON re.musicbrainzartistid = CAST(ar.musicbrainzartistid AS TEXT)
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
    
    
    public List<string> GetMissingTracksByArtistSpotify(string artistName)
    {
        string query = @"with original_artist AS(
							select id from spotify_artist
							where lower(name) = lower(@artistName) 
							order by popularity desc
							limit 1
						),
						unique_tracks AS (
							select distinct lower(ab.name) as album_title, lower(ar.name) as artist_name, lower(track.name) as track_title, ar.id as artist_id
							from spotify_artist ar
							left join spotify_album_artist ab_ar on ab_ar.artistid = ar.id
							left join spotify_album ab on ab.albumid = ab_ar.albumid
							left join spotify_track track on track.albumid = ab.albumid
						)
						SELECT distinct ut.artist_name || ' - ' || replace(ut.album_title, '-', '')  || ' - ' || replace(ut.track_title, '-', '')
						FROM unique_tracks ut
						left join original_artist orgArtist on 1=1

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

						where  ut.artist_id = orgArtist.id
						and m.metadataid is null";

        using var conn = new NpgsqlConnection(_connectionString);
        
        return conn
            .Query<string>(query, new
            {
                artistName
            }, commandTimeout: 60)
            .ToList();
    }
}