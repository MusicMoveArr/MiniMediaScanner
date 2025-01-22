using Npgsql;

namespace MiniMediaScanner.Repositories;

public class MusicBrainzReleaseTrackRepository
{
    private readonly string _connectionString;
    public MusicBrainzReleaseTrackRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public Guid InsertMusicBrainzReleaseTrack(
        string musicBrainzRemoteReleaseTrackId, 
        string musicBrainzRemoteRecordingTrackId, 
        string title, 
        string status,
        string musicBrainzRemoteReleaseId,
        int length,
        int number,
        int position,
        string recordingId,
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
        
        string query = @"INSERT INTO MusicBrainzReleaseTrack (MusicBrainzReleaseTrackId, MusicBrainzRemoteReleaseTrackId, 
                                     MusicBrainzRemoteRecordingTrackId, Title, Status, StatusId, MusicBrainzRemoteReleaseId,
                                     length, number, position, recordingid, recordinglength, recordingtitle, recordingvideo, mediatrackcount,
                                     mediaformat, mediatitle, mediaposition, mediatrackoffset)
                         VALUES (@id, @MusicBrainzRemoteReleaseTrackId, @MusicBrainzRemoteRecordingTrackId, @Title, @Status, @StatusId, @MusicBrainzRemoteReleaseId,
                                 @length, @number, @position, @recordingId, @recordingLength, @recordingTitle, @recordingvideo, @mediaTrackCount,
                                 @mediaFormat, @mediaTitle, @mediaPosition, @mediatrackoffset)
                         ON CONFLICT (MusicBrainzRemoteReleaseTrackId) 
                         DO UPDATE SET 
                             Title = EXCLUDED.Title, 
                             Status = EXCLUDED.Status, 
                             StatusId = EXCLUDED.StatusId, 
                             MusicBrainzRemoteRecordingTrackId = EXCLUDED.MusicBrainzRemoteRecordingTrackId,
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
                         RETURNING MusicBrainzReleaseTrackId";
        
        Guid releaseId = Guid.NewGuid();

        using var conn = new NpgsqlConnection(_connectionString);
        using var cmd = new NpgsqlCommand(query, conn);
        
        conn.Open();
        
        cmd.Parameters.AddWithValue("id", releaseId);
        cmd.Parameters.AddWithValue("MusicBrainzRemoteReleaseTrackId", musicBrainzRemoteReleaseTrackId);
        cmd.Parameters.AddWithValue("MusicBrainzRemoteRecordingTrackId", musicBrainzRemoteRecordingTrackId);
        cmd.Parameters.AddWithValue("Title", title);
        cmd.Parameters.AddWithValue("Status", status);
        cmd.Parameters.AddWithValue("StatusId", string.Empty);
        cmd.Parameters.AddWithValue("MusicBrainzRemoteReleaseId", musicBrainzRemoteReleaseId);
        cmd.Parameters.AddWithValue("length", length);
        cmd.Parameters.AddWithValue("number", number);
        cmd.Parameters.AddWithValue("position", position);
        cmd.Parameters.AddWithValue("recordingId", recordingId);
        cmd.Parameters.AddWithValue("recordingLength", recordingLength);
        cmd.Parameters.AddWithValue("recordingTitle", recordingTitle);
        cmd.Parameters.AddWithValue("recordingVideo", recordingVideo);
        cmd.Parameters.AddWithValue("mediaTrackCount", mediaTrackCount);
        cmd.Parameters.AddWithValue("mediaFormat", mediaFormat);
        cmd.Parameters.AddWithValue("mediaTitle", mediaTitle);
        cmd.Parameters.AddWithValue("mediaPosition", mediaPosition);
        cmd.Parameters.AddWithValue("mediatrackoffset", mediatrackoffset);

        var result = cmd.ExecuteScalar();
        if (result != null)
        {
            releaseId = (Guid)result;
        }

        return releaseId;
    }
}