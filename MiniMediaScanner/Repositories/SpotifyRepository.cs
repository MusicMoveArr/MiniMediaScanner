using Dapper;
using Npgsql;
using SpotifyAPI.Web;

namespace MiniMediaScanner.Repositories;

public class SpotifyRepository
{
    private readonly string _connectionString;
    public SpotifyRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public void InsertOrUpdateArtist(FullArtist artist)
    {
        string query = @"
            INSERT INTO spotify_artist (Id, 
                                  Name, 
                                  Popularity, 
                                  Type, 
                                  Uri, 
                                  TotalFollowers, 
                                  Href, 
                                  Genres, 
                                  lastsynctime)
            VALUES (@Id, @Name, @Popularity, @Type, 
                    @Uri, 
                    @TotalFollowers, 
                    @Href, 
                    @Genres, 
                    @lastsynctime)
            ON CONFLICT (Id)
            DO UPDATE SET
                Name = EXCLUDED.Name,
                Popularity = EXCLUDED.Popularity,
                Type = EXCLUDED.Type,
                Uri = EXCLUDED.Uri,
                TotalFollowers = EXCLUDED.TotalFollowers,
                Href = EXCLUDED.Href,
                Genres = EXCLUDED.Genres,
                lastsynctime = EXCLUDED.lastsynctime";

        using var conn = new NpgsqlConnection(_connectionString);
        
        conn.Execute(query, param: new
        {
            Id = artist.Id,
            Name = artist.Name,
            Popularity = artist.Popularity,
            Type = artist.Type,
            Uri = artist.Uri,
            TotalFollowers = artist.Followers.Total,
            Href = artist.Href,
            Genres = string.Join(',', artist.Genres),
            lastsynctime = DateTime.Now
        });
    }
    
    public void InsertOrUpdateArtistImage(FullArtist artist)
    {
        string query = @"
            INSERT INTO spotify_artist_image (ArtistId, 
                                  Height, 
                                  Width, 
                                  Url)
            VALUES (@ArtistId, @Height, @Width, @Url)
            ON CONFLICT (ArtistId, Height, Width)
            DO UPDATE SET
                Url = EXCLUDED.Url";

        using var conn = new NpgsqlConnection(_connectionString);

        foreach (var artistImage in artist.Images)
        {
            conn.Execute(query, param: new
            {
                ArtistId = artist.Id,
                Height = artistImage.Height,
                Width = artistImage.Width,
                Url = artistImage.Url
            });
        }
    }
    
    public void InsertOrUpdateAlbum(FullAlbum album, string albumGroup)
    {
        string query = @"
            INSERT INTO spotify_album (AlbumId, 
                                  AlbumGroup, 
                                  AlbumType, 
                                  Name, 
                                  ReleaseDate, 
                                  ReleaseDatePrecision, 
                                  TotalTracks, 
                                  Type, 
                                  Uri, 
                                  Label, 
                                  Popularity)
            VALUES (@AlbumId, @AlbumGroup, @AlbumType, @Name, @ReleaseDate, 
                    @ReleaseDatePrecision, @TotalTracks, @Type, @Uri,
                    @Label, @Popularity)
            ON CONFLICT (AlbumId)
            DO UPDATE SET
                AlbumGroup = EXCLUDED.AlbumGroup,
                AlbumType = EXCLUDED.AlbumType,
                Name = EXCLUDED.Name,
                ReleaseDate = EXCLUDED.ReleaseDate,
                ReleaseDatePrecision = EXCLUDED.ReleaseDatePrecision,
                TotalTracks = EXCLUDED.TotalTracks,
                Type = EXCLUDED.Type,
                Uri = EXCLUDED.Uri,
                Label = EXCLUDED.Label,
                Popularity = EXCLUDED.Popularity";

        using var conn = new NpgsqlConnection(_connectionString);

        conn.Execute(query, param: new
        {
            AlbumId = album.Id,
            AlbumGroup = albumGroup,
            AlbumType = album.AlbumType,
            Name = album.Name,
            ReleaseDate = album.ReleaseDate,
            ReleaseDatePrecision = album.ReleaseDatePrecision,
            TotalTracks = album.TotalTracks,
            Type = album.Type,
            Uri = album.Uri,
            Label = album.Label,
            Popularity = album.Popularity
        });
    }
    
    public void InsertOrUpdateAlbumImage(FullAlbum album)
    {
        string query = @"
            INSERT INTO spotify_album_image (AlbumId, 
                                  Height, 
                                  Width, 
                                  Url)
            VALUES (@AlbumId, @Height, @Width, @Url)
            ON CONFLICT (AlbumId, Height, Width)
            DO UPDATE SET
                Url = EXCLUDED.Url";

        using var conn = new NpgsqlConnection(_connectionString);

        foreach (var artistImage in album.Images)
        {
            conn.Execute(query, param: new
            {
                AlbumId = album.Id,
                Height = artistImage.Height,
                Width = artistImage.Width,
                Url = artistImage.Url
            });
        }
    }
    
    public void InsertOrUpdateAlbumArtist(FullAlbum album)
    {
        string query = @"
            INSERT INTO spotify_album_artist (AlbumId, 
                                  ArtistId, 
                                  Type)
            VALUES (@AlbumId, @ArtistId, @Type)
            ON CONFLICT (AlbumId, ArtistId, Type)
            DO  nothing
                ";

        using var conn = new NpgsqlConnection(_connectionString);

        foreach (var artist in album.Artists)
        {
            conn.Execute(query, param: new
            {
                AlbumId = album.Id,
                ArtistId = artist.Id,
                Type = album.Type
            });
        }
    }
    
    public void InsertOrUpdateAlbumExternalId(FullAlbum album)
    {
        string query = @"
            INSERT INTO spotify_album_externalid (AlbumId, 
                                  Name, 
                                  Value)
            VALUES (@AlbumId, @Name, @Value)
            ON CONFLICT (AlbumId, Name)
            DO UPDATE SET
                Value = EXCLUDED.Value";

        using var conn = new NpgsqlConnection(_connectionString);

        foreach (var externalId in album.ExternalIds)
        {
            conn.Execute(query, param: new
            {
                AlbumId = album.Id,
                Name = externalId.Key,
                Value = externalId.Value
            });
        }
    }
    
    public void InsertOrUpdateTrack(FullTrack track)
    {
        string query = @"
            INSERT INTO spotify_track (TrackId, 
                                  AlbumId, 
                                  DiscNumber, 
                                  DurationMs, 
                                  Explicit, 
                                  Href, 
                                  IsPlayable, 
                                  Name, 
                                  PreviewUrl, 
                                  TrackNumber, 
                                  Type, 
                                  Uri)
            VALUES (@TrackId, @AlbumId, @DiscNumber,
                    @DurationMs, @Explicit, @Href, @IsPlayable,
                    @Name, @PreviewUrl, @TrackNumber, @Type, @Uri)
            ON CONFLICT (TrackId, AlbumId)
            DO UPDATE SET
                DiscNumber = EXCLUDED.DiscNumber,
                DurationMs = EXCLUDED.DurationMs,
                Explicit = EXCLUDED.Explicit,
                Href = EXCLUDED.Href,
                IsPlayable = EXCLUDED.IsPlayable,
                Name = EXCLUDED.Name,
                PreviewUrl = EXCLUDED.PreviewUrl,
                TrackNumber = EXCLUDED.TrackNumber,
                Type = EXCLUDED.Type,
                Uri = EXCLUDED.Uri";

        using var conn = new NpgsqlConnection(_connectionString);

        conn.Execute(query, param: new
        {
            TrackId = track.Id,
            AlbumId = track.Album.Id,
            DiscNumber = track.DiscNumber,
            DurationMs = track.DurationMs,
            Explicit = track.Explicit,
            Href = track.Href,
            IsPlayable = track.IsPlayable,
            Name = track.Name,
            PreviewUrl = track.PreviewUrl ??string.Empty,
            TrackNumber = track.TrackNumber,
            Type = track.Type.ToString(),
            Uri = track.Uri
        });
    }
    
    public void InsertOrUpdateTrack_Artist(FullTrack track)
    {
        string query = @"
            INSERT INTO spotify_track_artist (TrackId, 
                                  ArtistId)
            VALUES (@TrackId, @ArtistId)
            ON CONFLICT (TrackId, ArtistId)
            DO  nothing
                ";

        using var conn = new NpgsqlConnection(_connectionString);

        foreach (var artist in track.Artists)
        {
            conn.Execute(query, param: new
            {
                TrackId = track.Id,
                ArtistId = artist.Id
            });
        }

    }
    public DateTime? GetArtistLastSyncTime(string artistId)
    {
        string query = @"SELECT lastsynctime FROM spotify_artist WHERE Id = @id";

        using var conn = new NpgsqlConnection(_connectionString);

        return conn.ExecuteScalar<DateTime>(query, new
        {
            id = artistId
        });
    }
    
    public void InsertOrUpdateTrackExternalId(FullTrack track)
    {
        string query = @"
            INSERT INTO spotify_track_externalid (TrackId, 
                                  Name, 
                                  Value)
            VALUES (@TrackId, @Name, @Value)
            ON CONFLICT (TrackId, Name)
            DO UPDATE SET
                Value = EXCLUDED.Value";

        using var conn = new NpgsqlConnection(_connectionString);

        foreach (var externalId in track.ExternalIds)
        {
            conn.Execute(query, param: new
            {
                TrackId = track.Id,
                Name = externalId.Key,
                Value = externalId.Value
            });
        }
    }
    
    public List<string> GetSpotifyArtistIdsByName(string artist)
    {
        string query = @"SELECT Id FROM spotify_artist where lower(name) = lower(@artist)";

        using var conn = new NpgsqlConnection(_connectionString);

        return conn
            .Query<string>(query, new
            {
                artist
            })
            .ToList();
    }
    
    public List<string> GetAllSpotifyArtistIds()
    {
        string query = @"SELECT Id FROM spotify_artist order by name asc";

        using var conn = new NpgsqlConnection(_connectionString);
        
        return conn
            .Query<string>(query)
            .ToList();
    }
}