using Dapper;
using MiniMediaScanner.Models.Spotify;
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
                Genres = EXCLUDED.Genres,
                lastsynctime = EXCLUDED.lastsynctime";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        await conn.ExecuteAsync(query, param: new
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

        await using var conn = new NpgsqlConnection(_connectionString);

        foreach (var artistImage in artist.Images)
        {
            await conn.ExecuteAsync(query, param: new
            {
                ArtistId = artist.Id,
                Height = artistImage.Height,
                Width = artistImage.Width,
                Url = artistImage.Url
            });
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

        await using var conn = new NpgsqlConnection(_connectionString);

        await conn.ExecuteAsync(query, param: new
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
        });
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

        await using var conn = new NpgsqlConnection(_connectionString);

        foreach (var artistImage in album.Images)
        {
            await conn.ExecuteAsync(query, param: new
            {
                AlbumId = album.Id,
                Height = artistImage.Height,
                Width = artistImage.Width,
                Url = artistImage.Url
            });
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

        await using var conn = new NpgsqlConnection(_connectionString);

        foreach (var artist in album.Artists)
        {
            await conn.ExecuteAsync(query, param: new
            {
                AlbumId = album.Id,
                ArtistId = artist.Id,
                Type = album.Type
            });
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

        await using var conn = new NpgsqlConnection(_connectionString);

        foreach (var externalId in album.ExternalIds)
        {
            await conn.ExecuteAsync(query, param: new
            {
                AlbumId = album.Id,
                Name = externalId.Key,
                Value = externalId.Value
            });
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

        await using var conn = new NpgsqlConnection(_connectionString);

        await conn.ExecuteAsync(query, param: new
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
    
    public async Task UpsertTrack_ArtistAsync(FullTrack track)
    {
        string query = @"
            INSERT INTO spotify_track_artist (TrackId, 
                                  ArtistId)
            VALUES (@TrackId, @ArtistId)
            ON CONFLICT (TrackId, ArtistId)
            DO  nothing
                ";

        await using var conn = new NpgsqlConnection(_connectionString);

        foreach (var artist in track.Artists)
        {
            await conn.ExecuteAsync(query, param: new
            {
                TrackId = track.Id,
                ArtistId = artist.Id
            });
        }

    }
    public async Task<DateTime?> GetArtistLastSyncTimeAsync(string artistId)
    {
        string query = @"SELECT lastsynctime FROM spotify_artist WHERE Id = @id";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.ExecuteScalarAsync<DateTime>(query, new
        {
            id = artistId
        });
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

        await using var conn = new NpgsqlConnection(_connectionString);

        foreach (var externalId in track.ExternalIds)
        {
            await conn.ExecuteAsync(query, param: new
            {
                TrackId = track.Id,
                Name = externalId.Key,
                Value = externalId.Value
            });
        }
    }
    
    public async Task<List<string>> GetSpotifyArtistIdsByNameAsync(string artist)
    {
        string query = @"SELECT Id FROM spotify_artist where lower(name) = lower(@artist)";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn
            .QueryAsync<string>(query, new
            {
                artist
            }))
            .ToList();
    }
    
    public async Task<List<string>> GetAllSpotifyArtistIdsAsync()
    {
        string query = @"SELECT Id FROM spotify_artist order by name asc";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        return (await conn
            .QueryAsync<string>(query))
            .ToList();
    }

    public async Task<List<SpotifyTrackModel>> GetTrackByArtistIdAsync(string artistId, string albumName, string trackName)
    {
        string query = @"select
                             track.name As TrackName,
	                         track.TrackId,
	                         track.AlbumId,
	                         track.DiscNumber,
	                         (track.durationms / 1000.0) * interval '1 second' as Duration,
	                         track.Explicit,
	                         track.Href as TrackHref,
	                         track.TrackNumber,
	                         track.Uri,
	                         album.AlbumGroup,
	                         album.AlbumType,
	                         album.ReleaseDate,
	                         album.TotalTracks,
	                         album.Label,
	                         album.name as AlbumName,
	                         artist.Href as ArtistHref,
	                         artist.Genres,
	                         artist.name as ArtistName,
	                         artist.id as ArtistId
                         from spotify_track track
                         join spotify_album album on album.albumid = track.albumid and album.albumgroup in ('album', 'single') and album.albumtype in ('album', 'single')
                         join spotify_track_artist track_artist on track_artist.trackid = track.trackid
                         join spotify_album_artist album_artist on album_artist.albumid = album.albumid 
                         join spotify_artist artist on artist.id = track_artist.artistid or 
						 	                           artist.id = album_artist.artistid
                         where artist.id = @artistId
	                         and (length(@albumName) = 0 OR similarity(lower(album.name), lower(@albumName)) >= 0.8)
	                         and (length(@trackName) = 0 OR similarity(lower(track.name), lower(@trackName)) >= 0.8)";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        return (await conn
            .QueryAsync<SpotifyTrackModel>(query,
                param: new
                {
                    artistId,
                    albumName,
                    trackName
                }))
            .ToList();
    }

    public async Task<List<SpotifyExternalValue>> GetTrackExternalValuesAsync(string trackId)
    {
        string query = @"select
	                         trackid as Id,
	                         Name,
	                         Value
                         from spotify_track_externalid externalvalue
                         where externalvalue.trackid = @trackId";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        return (await conn
            .QueryAsync<SpotifyExternalValue>(query,
                param: new
                {
                    trackId
                }))
            .ToList();
    }
    public async Task<List<SpotifyExternalValue>> GetAlbumExternalValuesAsync(string albumId)
    {
        string query = @"select
	                         albumid as Id,
	                         Name,
	                         Value
                         from spotify_album_externalid externalvalue
                         where externalvalue.albumid = @albumId";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        return (await conn
            .QueryAsync<SpotifyExternalValue>(query,
                param: new
                {
                    albumId
                }))
            .ToList();
    }
    public async Task<List<string>> GetTrackArtistsAsync(string trackId)
    {
        string query = @"SELECT artist.name
                         FROM spotify_track_artist sta
                         join spotify_artist artist on artist.id = sta.artistid
                         where sta.trackid = @trackId";

        await using var conn = new NpgsqlConnection(_connectionString);
        
        return (await conn
            .QueryAsync<string>(query,
                param: new
                {
                    trackId
                }))
            .ToList();
    }
    
    public async Task<string?> GetHighestQualityAlbumCoverUrlAsync(string artistId, string albumName)
    {
        string query = @"SELECT sai.url
                         FROM spotify_album_image sai
                         join spotify_album album on album.albumid = sai.albumid and lower(album.name) = lower(@albumName)
                         join spotify_album_artist saa on saa.artistid = @artistId and saa.albumid = sai.albumid
                         order by sai.height desc, sai.width desc
                         limit 1";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn
            .QueryFirstOrDefaultAsync<string>(query,
                param: new
                {
                    artistId,
                    albumName
                });
    }
    
    public async Task<string?> GetHighestQualityArtistCoverUrlAsync(string artistId)
    {
        string query = @"SELECT url
                         FROM spotify_artist_image sai
                         where sai.artistid = @artistId
                         order by height desc, width desc
                         limit 1";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn
            .QueryFirstOrDefaultAsync<string>(query,
                param: new
                {
                    artistId
                });
    }
    
    public async Task<bool> SpotifyAlbumIdExistsAsync(string albumId)
    {
        string query = @"SELECT 1
                         FROM spotify_album album
                         where album.albumid = @albumId
                         limit 1";

        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn
            .ExecuteScalarAsync<int?>(query,
                param: new
                {
                    albumId
                })) == 1;
    }
    
    public async Task<int> GetAlbumTrackCountAsync(string albumId)
    {
        string query = @"SELECT count(track.trackid)
                         FROM spotify_album album
                         join spotify_track track on track.albumid = album.albumid
                         where album.albumid = @albumId
                         limit 1";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn
            .ExecuteScalarAsync<int>(query,
                param: new
                {
                    albumId
                });
    }
}