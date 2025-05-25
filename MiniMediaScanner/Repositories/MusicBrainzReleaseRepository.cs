using Dapper;
using Npgsql;

namespace MiniMediaScanner.Repositories;

public class MusicBrainzReleaseRepository
{
    private readonly string _connectionString;
    public MusicBrainzReleaseRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    

    public async Task<Guid> UpsertReleaseAsync(Guid artistId, 
        Guid releaseId, 
        string title, 
        string? status, 
        string? statusId,
        string? date,
        string? barcode,
        string? country,
        string? disambiguation,
        string? quality)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            status = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(statusId))
        {
            statusId = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(date))
        {
            date = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(barcode))
        {
            barcode = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(country))
        {
            country = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(disambiguation))
        {
            disambiguation = string.Empty;
        }
        if (string.IsNullOrWhiteSpace(quality))
        {
            quality = string.Empty;
        }
        
        string query = @"INSERT INTO MusicBrainz_Release (ArtistId, ReleaseId,
                                Title, Status, StatusId,
                                Date,
                                Barcode,
                                Country,
                                Disambiguation,
                                Quality)
                         VALUES (@artistId, @releaseId, @Title, @Status, @StatusId, @Date, @Barcode, @Country, @Disambiguation, @Quality)
                         ON CONFLICT (ArtistId, ReleaseId) 
                         DO UPDATE SET 
                             Title = EXCLUDED.Title, 
                             Status = EXCLUDED.Status, 
                             StatusId = EXCLUDED.StatusId, 
                             Date = EXCLUDED.Date, 
                             Barcode = EXCLUDED.Barcode, 
                             Country = EXCLUDED.Country, 
                             Disambiguation = EXCLUDED.Disambiguation, 
                             Quality = EXCLUDED.Quality
                         RETURNING ReleaseId";
        
        await using var conn = new NpgsqlConnection(_connectionString);
        
        return await conn.ExecuteScalarAsync<Guid>(query, new
            {
                artistId,
                releaseId,
                Title = title,
                Status = status,
                StatusId = statusId,
                Date = date,
                Barcode = barcode,
                Country = country,
                Disambiguation = disambiguation,
                Quality = quality
            });
    }
    
    public async Task<bool> ReleaseIdExistsAsync(Guid releaseId)
    {
        string query = @"SELECT 1
                         FROM MusicBrainz_Release album
                         where album.ReleaseId = @releaseId
                         limit 1";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn
            .ExecuteScalarAsync<int?>(query,
                param: new
                {
                    releaseId
                })) == 1;
    }
}