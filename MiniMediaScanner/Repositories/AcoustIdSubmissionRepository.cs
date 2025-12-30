using Npgsql;
using Dapper;
using MiniMediaScanner.Models;

namespace MiniMediaScanner.Repositories;

public class AcoustIdSubmissionRepository
{
    private readonly string _connectionString;
    public AcoustIdSubmissionRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<List<MetadataModel>> GetMetadataNotSubmittedAsync()
    {
        string query = @$"SELECT distinct on (m.MetadataId)
                                 m.MetadataId, 
                                 m.Path, 
                                 m.Title, 
                                 m.AlbumId,
                                 album.title AS AlbumName,
                                 tag_track,
                                 tag_trackcount,
                                 tag_disc,
                                 tag_disccount,
                                 artist.name AS ArtistName,
                                 m.MusicBrainzArtistId,
                                 m.tag_acoustid,
                                 m.Tag_AllJsonTags,
                                 artist.ArtistId,
                                 m.Tag_Isrc,
                                 Tag_AcoustIdFingerprint,
                                 CASE
                                    WHEN m.Tag_Length !~ ':' THEN NULL 
                                    WHEN m.Tag_Length ~ '^\d{{1,2}}:\d{{2}}$' THEN ('0:' || m.Tag_Length)::interval
                                    ELSE m.Tag_Length::interval
                                  END AS TrackLength,
                                 m.Tag_AllJsonTags->>upcQuery.key AS Tag_Upc,
                                 m.Tag_AllJsonTags->>dateQuery.key AS Tag_Date
                        FROM metadata m
                        JOIN albums album ON album.albumid = m.albumid
                        JOIN artists artist ON artist.artistid = album.artistid
                        left JOIN LATERAL (
	                         SELECT jsonb_object_keys(m.tag_alljsontags) AS key
	                     ) upcQuery ON lower(upcQuery.key) = 'upc'
                        left JOIN LATERAL (
	                         SELECT jsonb_object_keys(m.tag_alljsontags) AS key
	                     ) dateQuery ON lower(dateQuery.key) = 'date'
                        left join acoustid_submission sub on sub.metadataid = m.metadataid
                        where sub.metadataid is null and length(Tag_AcoustIdFingerprint) > 0
                        limit 5000";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        return conn.Query<MetadataModel>(query).ToList();
    }
    
    public async Task<List<int>> GetCheckMetadataIdsAsync()
    {
        string query = @$"SELECT submissionid
                          FROM acoustid_submission
                          where status = 'pending'
                          limit 5000";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn.QueryAsync<int>(query)).ToList();
    }
    
    public async Task UpdateSubmissionStatusAsync(int submissionId, string status, Guid importId)
    {
        string query = @$"UPDATE acoustid_submission
                          SET status = @status, importId = @importId
                          WHERE submissionId = @submissionId";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        await conn.ExecuteAsync(query, new
        {
            submissionId,
            status,
            importId
        });
    }
}