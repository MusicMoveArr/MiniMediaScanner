using System.Data;
using Dapper;
using MiniMediaScanner.Models.Spotify;
using Npgsql;
using SpotifyAPI.Web;

namespace MiniMediaScanner.Repositories;

public class UpdateSpotifyRepository : BaseUpdateRepository
{
    public UpdateSpotifyRepository(string connectionString)
        : base(connectionString)
    {
        
    }

    public async Task UpsertArtistAsync(FullArtist artist)
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
                Genres = EXCLUDED.Genres";
        
        await base.Connection.ExecuteAsync(query, param: new
        {
            Id = artist.Id,
            Name = artist.Name,
            Popularity = artist.Popularity,
            Type = artist.Type,
            Uri = artist.Uri,
            TotalFollowers = artist.Followers.Total,
            Href = artist.Href,
            Genres = string.Join(',', artist.Genres),
            lastsynctime = new DateTime(2000, 1,1)
        }, transaction: base.Transaction);
    }
    
    public async Task UpsertArtistImageAsync(FullArtist artist)
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

        foreach (var artistImage in artist.Images)
        {
            await base.Connection.ExecuteAsync(query, param: new
            {
                ArtistId = artist.Id,
                Height = artistImage.Height,
                Width = artistImage.Width,
                Url = artistImage.Url
            }, transaction: base.Transaction);
        }
    }
    
    public async Task UpsertAlbumAsync(FullAlbum album, string albumGroup, string artistId)
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
                                  Popularity,
                                  ArtistId)
            VALUES (@AlbumId, @AlbumGroup, @AlbumType, @Name, @ReleaseDate, 
                    @ReleaseDatePrecision, @TotalTracks, @Type, @Uri,
                    @Label, @Popularity, @artistId)
            ON CONFLICT (AlbumId, ArtistId)
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
                Popularity = EXCLUDED.Popularity,
                ArtistId = EXCLUDED.ArtistId";

        await base.Connection.ExecuteAsync(query, param: new
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
            Popularity = album.Popularity,
            artistId
        }, transaction: base.Transaction);
    }
    
    public async Task UpsertAlbumImageAsync(FullAlbum album)
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

        foreach (var artistImage in album.Images)
        {
            await base.Connection.ExecuteAsync(query, param: new
            {
                AlbumId = album.Id,
                Height = artistImage.Height,
                Width = artistImage.Width,
                Url = artistImage.Url
            }, transaction: base.Transaction);
        }
    }
    
    public async Task UpsertAlbumArtistAsync(FullAlbum album)
    {
        string query = @"
            INSERT INTO spotify_album_artist (AlbumId, 
                                  ArtistId, 
                                  Type)
            VALUES (@AlbumId, @ArtistId, @Type)
            ON CONFLICT (AlbumId, ArtistId, Type)
            DO  nothing
                ";

        foreach (var artist in album.Artists)
        {
            await base.Connection.ExecuteAsync(query, param: new
            {
                AlbumId = album.Id,
                ArtistId = artist.Id,
                Type = album.Type
            }, transaction: base.Transaction);
        }
    }
    
    public async Task UpsertAlbumExternalIdAsync(FullAlbum album)
    {
        string query = @"
            INSERT INTO spotify_album_externalid (AlbumId, 
                                  Name, 
                                  Value)
            VALUES (@AlbumId, @Name, @Value)
            ON CONFLICT (AlbumId, Name)
            DO UPDATE SET
                Value = EXCLUDED.Value";

        foreach (var externalId in album.ExternalIds)
        {
            await base.Connection.ExecuteAsync(query, param: new
            {
                AlbumId = album.Id,
                Name = externalId.Key,
                Value = externalId.Value
            }, transaction: base.Transaction);
        }
    }
    
    public async Task UpsertTrackAsync(FullTrack track)
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

        await base.Connection.ExecuteAsync(query, param: new
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
        }, transaction: base.Transaction);
    }
    
    public async Task UpsertTrack_ArtistAsync(FullTrack track)
    {
        string query = @"
            INSERT INTO spotify_track_artist (TrackId, 
                                  ArtistId)
            VALUES (@TrackId, @ArtistId)
            ON CONFLICT (TrackId, ArtistId)
            DO  nothing
                ";

        foreach (var artist in track.Artists)
        {
            await base.Connection.ExecuteAsync(query, param: new
            {
                TrackId = track.Id,
                ArtistId = artist.Id
            }, transaction: base.Transaction);
        }
    }
    
    public async Task<DateTime?> GetArtistLastSyncTimeAsync(string artistId)
    {
        string query = @"SELECT lastsynctime FROM spotify_artist WHERE Id = @id";

        return await base.Connection.ExecuteScalarAsync<DateTime>(query, new
        {
            id = artistId
        }, transaction: base.Transaction);
    }
    
    public async Task<DateTime?> SetArtistLastSyncTimeAsync(string artistId)
    {
        string query = @"UPDATE spotify_artist SET lastsynctime = @lastsynctime WHERE Id = @id";

        return await base.Connection.ExecuteScalarAsync<DateTime>(query, new
        {
            id = artistId,
            lastsynctime = DateTime.Now
        }, transaction: base.Transaction);
    }
    
    public async Task UpsertTrackExternalIdAsync(FullTrack track)
    {
        string query = @"
            INSERT INTO spotify_track_externalid (TrackId, 
                                  Name, 
                                  Value)
            VALUES (@TrackId, @Name, @Value)
            ON CONFLICT (TrackId, Name)
            DO UPDATE SET
                Value = EXCLUDED.Value";

        foreach (var externalId in track.ExternalIds)
        {
            await base.Connection.ExecuteAsync(query, param: new
            {
                TrackId = track.Id,
                Name = externalId.Key,
                Value = externalId.Value
            }, transaction: base.Transaction);
        }
    }
    
    public async Task<int> GetAlbumTrackCountAsync(string albumId)
    {
        string query = @"SELECT count(track.trackid)
                         FROM spotify_album album
                         join spotify_track track on track.albumid = album.albumid
                         where album.albumid = @albumId
                         limit 1";

        return await base.Connection
            .ExecuteScalarAsync<int>(query,
                param: new
                {
                    albumId
                }, transaction: base.Transaction);
    }
}