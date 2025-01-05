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
        string musicBrainzRemoteReleaseId)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            status = string.Empty;
        }
        
        string query = @"INSERT INTO MusicBrainzReleaseTrack (MusicBrainzReleaseTrackId, MusicBrainzRemoteReleaseTrackId, MusicBrainzRemoteRecordingTrackId, Title, Status, StatusId, MusicBrainzRemoteReleaseId)
                         VALUES (@id, @MusicBrainzRemoteReleaseTrackId, @MusicBrainzRemoteRecordingTrackId, @Title, @Status, @StatusId, @MusicBrainzRemoteReleaseId)
                         ON CONFLICT (MusicBrainzRemoteReleaseTrackId) 
                         DO UPDATE SET 
                             Title = EXCLUDED.Title, 
                             Status = EXCLUDED.Status, 
                             StatusId = EXCLUDED.StatusId, 
                             MusicBrainzRemoteRecordingTrackId = EXCLUDED.MusicBrainzRemoteRecordingTrackId,
                             MusicBrainzRemoteReleaseId = EXCLUDED.MusicBrainzRemoteReleaseId
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

        var result = cmd.ExecuteScalar();
        if (result != null)
        {
            releaseId = (Guid)result;
        }

        return releaseId;
    }
}