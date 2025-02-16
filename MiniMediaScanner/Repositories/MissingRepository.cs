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
                         where lower(ar.name) = lower(@artistName)";
        
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
}