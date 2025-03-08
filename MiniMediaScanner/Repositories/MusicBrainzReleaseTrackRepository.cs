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

    public Guid InsertMusicBrainzReleaseTrack(
        Guid musicBrainzRemoteReleaseTrackId, 
        Guid musicBrainzRemoteRecordingTrackId, 
        string title, 
        string status,
        Guid musicBrainzRemoteReleaseId,
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
        
        return conn.ExecuteScalar<Guid>(query, new
            {
                Id = releaseId,
                MusicBrainzRemoteReleaseTrackId = musicBrainzRemoteReleaseTrackId,
                MusicBrainzRemoteRecordingTrackId = musicBrainzRemoteRecordingTrackId,
                Title = title,
                Status = status,
                StatusId = status,
                MusicBrainzRemoteReleaseId = musicBrainzRemoteReleaseId,
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