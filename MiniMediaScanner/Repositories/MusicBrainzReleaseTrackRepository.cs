using Dapper;
using Npgsql;

namespace MiniMediaScanner.Repositories;

public class MusicBrainzReleaseTrackRepository
{
    private readonly string _connectionString;
    public MusicBrainzReleaseTrackRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Guid> UpsertReleaseTrackAsync(
        Guid releaseTrackId, 
        Guid recordingTrackId, 
        string title, 
        string status,
        string statusId,
        Guid releaseId,
        int length,
        int number,
        int position,
        Guid recordingId,
        int recordingLength,
        string recordingTitle,
        bool recordingVideo,
        int mediaTrackCount,
        string mediaFormat,
        string mediaTitle,
        int mediaPosition,
        int mediatrackoffset)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            status = string.Empty;
        }
        
        string query = @"INSERT INTO MusicBrainz_Release_Track (ReleaseTrackId, RecordingTrackId, 
                                     Title, Status, StatusId, ReleaseId,
                                     length, number, position, recordingid, recordinglength, recordingtitle, recordingvideo, mediatrackcount,
                                     mediaformat, mediatitle, mediaposition, mediatrackoffset)
                         VALUES (@releaseTrackId, @recordingTrackId, @title, @status, @statusId, @releaseId,
                                 @length, @number, @position, @recordingId, @recordingLength, @recordingTitle, @recordingvideo, @mediaTrackCount,
                                 @mediaFormat, @mediaTitle, @mediaPosition, @mediatrackoffset)
                         ON CONFLICT (ReleaseTrackId) 
                         DO UPDATE SET 
                             Title = EXCLUDED.Title, 
                             Status = EXCLUDED.Status, 
                             StatusId = EXCLUDED.StatusId, 
                             RecordingTrackId = EXCLUDED.RecordingTrackId,
                             length = EXCLUDED.length,
                             number = EXCLUDED.number,
                             position = EXCLUDED.position,
                             recordingid = EXCLUDED.recordingid,
                             recordinglength = EXCLUDED.recordinglength,
                             recordingtitle = EXCLUDED.recordingtitle,
                             recordingvideo = EXCLUDED.recordingvideo,
                             mediatrackcount = EXCLUDED.mediatrackcount,
                             mediaformat = EXCLUDED.mediaformat,
                             mediatitle = EXCLUDED.mediatitle,
                             mediaposition = EXCLUDED.mediaposition,
                             mediatrackoffset = EXCLUDED.mediatrackoffset
                         RETURNING ReleaseTrackId";
        
        await using var conn = new NpgsqlConnection(_connectionString);
        
        return await conn.ExecuteScalarAsync<Guid>(query, new
            {
                releaseTrackId,
                recordingTrackId,
                title,
                status,
                statusId,
                releaseId,
                length,
                number,
                position,
                recordingId,
                recordingLength,
                recordingTitle,
                recordingVideo,
                mediaTrackCount,
                mediaFormat,
                mediaTitle,
                mediaPosition,
                mediatrackoffset
            });
    }
}