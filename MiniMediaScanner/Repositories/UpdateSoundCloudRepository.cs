using Dapper;
using MiniMediaScanner.Models.Tidal;
using Npgsql;
using SoundCloudExplode.Playlists;
using SoundCloudExplode.Search;
using SoundCloudExplode.Tracks;

namespace MiniMediaScanner.Repositories;

public class UpdateSoundCloudRepository : BaseUpdateRepository
{
    public UpdateSoundCloudRepository(string connectionString)
        : base(connectionString)
    {
        
    }
    
    public async Task<DateTime?> GetArtistLastSyncTimeAsync(long artistId)
    {
        string query = @"SELECT LastSyncTime FROM soundcloud_user WHERE Id = @id";

        return await base.Connection
            .ExecuteScalarAsync<DateTime>(query, new
            {
                id = artistId
            }, transaction: base.Transaction);
    }
    
    public async Task<DateTime?> SetArtistLastSyncTimeAsync(long artistId)
    {
        string query = @"UPDATE soundcloud_user SET LastSyncTime = @lastsynctime WHERE Id = @id";

        return await base.Connection
            .ExecuteScalarAsync<DateTime>(query, new
            {
                id = artistId,
                lastsynctime = DateTime.Now
            }, transaction: base.Transaction);
    }
    
    public async Task UpsertUserAsync(UserSearchResult userSearchResult)
    {
        string query = @"
            INSERT INTO soundcloud_user (Id, 
                                  Title, 
                                  FirstName, 
                                  LastName, 
                                  FullName, 
                                  CountryCode, 
                                  City, 
                                  AvatarUrl, 
                                  PermaLink, 
                                  Url, 
                                  Urn, 
                                  Username, 
                                  Badge_ProUnlimited, 
                                  Badge_Verified, 
                                  LastModified, 
                                  LastSyncTime)
            VALUES (@Id, @Title, @FirstName, @LastName, @FullName, @CountryCode, @City, @AvatarUrl,
                    @PermaLink, @Url, @Urn, @Username, @Badge_ProUnlimited, @Badge_Verified,
                    @LastModified, @LastSyncTime)
            ON CONFLICT (Id)
            DO UPDATE SET
                Title = EXCLUDED.Title,
                FirstName = EXCLUDED.FirstName,
                LastName = EXCLUDED.LastName,
                FullName = EXCLUDED.FullName,
                CountryCode = EXCLUDED.CountryCode,
                City = EXCLUDED.City,
                AvatarUrl = EXCLUDED.AvatarUrl,
                PermaLink = EXCLUDED.PermaLink,
                Url = EXCLUDED.Url,
                Urn = EXCLUDED.Urn,
                Username = EXCLUDED.Username,
                Badge_ProUnlimited = EXCLUDED.Badge_ProUnlimited,
                Badge_Verified = EXCLUDED.Badge_Verified,
                LastModified = EXCLUDED.LastModified";

        await base.Connection
            .ExecuteAsync(query, param: new
            {
                Id = userSearchResult.Id,
                Title = userSearchResult.Title ?? string.Empty,
                FirstName = userSearchResult.FirstName ?? string.Empty,
                LastName = userSearchResult.LastName ?? string.Empty,
                FullName = userSearchResult.FullName ?? string.Empty,
                CountryCode = userSearchResult.CountryCode ?? string.Empty,
                City = userSearchResult.City ?? string.Empty,
                AvatarUrl = userSearchResult.AvatarUrl?.ToString() ?? string.Empty,
                PermaLink = userSearchResult.Permalink ?? string.Empty,
                Url = userSearchResult.Url ?? string.Empty,
                Urn = userSearchResult.Urn ?? string.Empty,
                Username = userSearchResult.Username ?? string.Empty,
                Badge_ProUnlimited = userSearchResult.Badges?.ProUnlimited ?? false,
                Badge_Verified = userSearchResult.Badges?.Verified ?? false,
                LastModified = userSearchResult.LastModified?.DateTime,
                lastsynctime = new DateTime(2000, 1, 1)
            }, transaction: base.Transaction);
    }
    
    public async Task UpsertPlaylistAsync(Playlist playlist)
    {
        string query = @"
            INSERT INTO soundcloud_playlist (Id, 
                                  UserId, 
                                  Title, 
                                  Description, 
                                  Duration, 
                                  EmbeddableBy, 
                                  Genre, 
                                  LabelName, 
                                  License, 
                                  LikesCount, 
                                  ManagedByFeeds, 
                                  Public, 
                                  PurchaseTitle, 
                                  PurchaseUrl, 
                                  ReleaseDate, 
                                  RepostsCount, 
                                  Sharing, 
                                  TagList, 
                                  SetType, 
                                  IsAlbum, 
                                  PublishedAt, 
                                  TrackCount, 
                                  Uri, 
                                  PermalinkUrl, 
                                  Permalink, 
                                  ArtworkUrl, 
                                  DisplayDate, 
                                  CreatedAt, 
                                  LastModified)
            VALUES (@Id, @UserId, @Title, @Description, @Duration, @EmbeddableBy, @Genre, @LabelName, @License,
                    @LikesCount, @ManagedByFeeds, @Public, @PurchaseTitle, @PurchaseUrl, @ReleaseDate, @RepostsCount, @Sharing, @TagList, @SetType,
                    @IsAlbum, @PublishedAt, @TrackCount, @Uri, @PermalinkUrl, @Permalink, @ArtworkUrl, @DisplayDate,
                    @CreatedAt, @LastModified)
            ON CONFLICT (Id, UserId)
            DO UPDATE SET
                Title = EXCLUDED.Title,
                Description = EXCLUDED.Description,
                Duration = EXCLUDED.Duration,
                EmbeddableBy = EXCLUDED.EmbeddableBy,
                Genre = EXCLUDED.Genre,
                LabelName = EXCLUDED.LabelName,
                License = EXCLUDED.License,
                LikesCount = EXCLUDED.LikesCount,
                ManagedByFeeds = EXCLUDED.ManagedByFeeds,
                Public = EXCLUDED.Public,
                PurchaseTitle = EXCLUDED.PurchaseTitle,
                PurchaseUrl = EXCLUDED.PurchaseUrl,
                ReleaseDate = EXCLUDED.ReleaseDate,
                RepostsCount = EXCLUDED.RepostsCount,
                Sharing = EXCLUDED.Sharing,
                TagList = EXCLUDED.TagList,
                SetType = EXCLUDED.SetType,
                CreatedAt = EXCLUDED.CreatedAt,
                LastModified = EXCLUDED.LastModified";

        DateTime.TryParse(playlist.PublishedAt?.ToString(), out DateTime publishedAt);
        DateTime.TryParse(playlist.ReleaseDate?.ToString(), out DateTime releaseDate);
        
        await base.Connection
            .ExecuteAsync(query, param: new
            {
                Id = playlist.Id.Value,
                UserId = playlist.UserId,
                Title = CleanupInvalidChars(playlist.Title),
                Description = CleanupInvalidChars(playlist.Description),
                Duration = playlist.Duration,
                EmbeddableBy = CleanupInvalidChars(playlist.EmbeddableBy),
                Genre = CleanupInvalidChars(playlist.Genre),
                LabelName = CleanupInvalidChars(playlist.LabelName),
                License = CleanupInvalidChars(playlist.License),
                LikesCount = playlist.LikesCount,
                ManagedByFeeds = playlist.ManagedByFeeds,
                Public = playlist.Public,
                PurchaseTitle = CleanupInvalidChars(playlist.PurchaseTitle),
                PurchaseUrl = CleanupInvalidChars(playlist.PurchaseUrl),
                ReleaseDate = releaseDate,
                RepostsCount = playlist.RepostsCount,
                Sharing = CleanupInvalidChars(playlist.Sharing),
                TagList = CleanupInvalidChars(playlist.TagList),
                SetType = CleanupInvalidChars(playlist.SetType),
                IsAlbum = playlist.IsAlbum,
                PublishedAt = publishedAt,
                TrackCount = playlist.TrackCount ?? 0,
                Uri = CleanupInvalidChars(playlist.Uri?.ToString()),
                PermalinkUrl = CleanupInvalidChars(playlist.PermalinkUrl?.ToString()),
                Permalink = CleanupInvalidChars(playlist.Permalink),
                ArtworkUrl = CleanupInvalidChars(playlist.ArtworkUrl?.ToString()),
                DisplayDate = playlist.DisplayDate.DateTime,
                CreatedAt = playlist.CreatedAt.DateTime,
                LastModified = playlist.LastModified.DateTime
            }, transaction: base.Transaction);
    }
    
    
    public async Task UpsertTrackAsync(Track track)
    {
        string query = @"
            INSERT INTO soundcloud_track (Id, 
                                  UserId, 
                                  Title, 
                                  PlaylistName, 
                                  Caption, 
                                  Commentable, 
                                  CommentCount, 
                                  Description, 
                                  Downloadable, 
                                  DownloadCount, 
                                  Duration, 
                                  FullDuration,
                                  EmbeddableBy,
                                  Genre,
                                  HasDownloadsLeft,
                                  LabelName,
                                  License,
                                  LikesCount,
                                  Permalink,
                                  PermalinkUrl,
                                  PlaybackCount,
                                  Public,
                                  PublisherMetadata_Artist,
                                  PublisherMetadata_ContainsMusic,
                                  PublisherMetadata_Id,
                                  PublisherMetadata_Urn,
                                  PurchaseTitle,
                                  PurchaseUrl,
                                  ReleaseDate,
                                  RepostsCount,
                                  Sharing,
                                  State,
                                  Streamable,
                                  TagList,
                                  Uri,
                                  ArtworkUrl,
                                  Visuals,
                                  WaveformUrl,
                                  DisplayDate,
                                  MonetizationModel,
                                  Policy,
                                  Urn,
                                  CreatedAt,
                                  LastModified)
            VALUES (@Id, @UserId, @Title, @PlaylistName, @Caption, @Commentable, @CommentCount, 
                   @Description, @Downloadable, @DownloadCount, @Duration, @FullDuration,@EmbeddableBy,
                   @Genre, @HasDownloadsLeft, @LabelName, @License, @LikesCount, @Permalink, @PermalinkUrl,
                   @PlaybackCount, @Public, @PublisherMetadata_Artist, @PublisherMetadata_ContainsMusic,
                   @PublisherMetadata_Id, @PublisherMetadata_Urn, @PurchaseTitle, @PurchaseUrl, @ReleaseDate,
                   @RepostsCount, @Sharing, @State, @Streamable, @TagList, @Uri, @ArtworkUrl, @Visuals,
                   @WaveformUrl, @DisplayDate, @MonetizationModel, @Policy, @Urn, @CreatedAt, @LastModified)
            ON CONFLICT (Id, UserId)
            DO UPDATE SET
              Title = EXCLUDED.Title,
              PlaylistName = EXCLUDED.PlaylistName,
              Caption = EXCLUDED.Caption,
              Commentable = EXCLUDED.Commentable,
              CommentCount = EXCLUDED.CommentCount, 
              Description = EXCLUDED.Description, 
              Downloadable = EXCLUDED.Downloadable, 
              DownloadCount = EXCLUDED.DownloadCount, 
              Duration = EXCLUDED.Duration, 
              FullDuration = EXCLUDED.FullDuration,
              EmbeddableBy = EXCLUDED.EmbeddableBy,
              Genre = EXCLUDED.Genre,
              HasDownloadsLeft = EXCLUDED.HasDownloadsLeft,
              LabelName = EXCLUDED.LabelName,
              License = EXCLUDED.License,
              LikesCount = EXCLUDED.LikesCount,
              Permalink = EXCLUDED.Permalink,
              PermalinkUrl = EXCLUDED.PermalinkUrl,
              PlaybackCount = EXCLUDED.PlaybackCount,
              Public = EXCLUDED.Public,
              PublisherMetadata_Artist = EXCLUDED.PublisherMetadata_Artist,
              PublisherMetadata_ContainsMusic = EXCLUDED.PublisherMetadata_ContainsMusic,
              PublisherMetadata_Id = EXCLUDED.PublisherMetadata_Id,
              PublisherMetadata_Urn = EXCLUDED.PublisherMetadata_Urn,
              PurchaseTitle = EXCLUDED.PurchaseTitle,
              PurchaseUrl = EXCLUDED.PurchaseUrl,
              ReleaseDate = EXCLUDED.ReleaseDate,
              RepostsCount = EXCLUDED.RepostsCount,
              Sharing = EXCLUDED.Sharing,
              State = EXCLUDED.State,
              Streamable = EXCLUDED.Streamable,
              TagList = EXCLUDED.TagList,
              Uri = EXCLUDED.Uri,
              ArtworkUrl = EXCLUDED.ArtworkUrl,
              Visuals = EXCLUDED.Visuals,
              WaveformUrl = EXCLUDED.WaveformUrl,
              DisplayDate = EXCLUDED.DisplayDate,
              MonetizationModel = EXCLUDED.MonetizationModel,
              Policy = EXCLUDED.Policy,
              Urn = EXCLUDED.Urn,
              CreatedAt = EXCLUDED.CreatedAt,
              LastModified = EXCLUDED.LastModified";

        DateTime.TryParse(track.ReleaseDate?.ToString(), out DateTime releaseDate);
        
        await base.Connection
            .ExecuteAsync(query, param: new
            {
                Id = track.Id, 
                UserId = track.UserId, 
                Title = CleanupInvalidChars(track.Title), 
                PlaylistName = CleanupInvalidChars(track.PlaylistName), 
                Caption = CleanupInvalidChars(track.Caption?.ToString()), 
                Commentable = track.Commentable, 
                CommentCount = track.CommentCount ?? 0, 
                Description = CleanupInvalidChars(track.Description), 
                Downloadable = track.Downloadable, 
                DownloadCount = track.DownloadCount ?? 0, 
                Duration = track.Duration ?? 0, 
                FullDuration = track.FullDuration ?? 0,
                EmbeddableBy = CleanupInvalidChars(track.EmbeddableBy),
                Genre = CleanupInvalidChars(track.Genre),
                HasDownloadsLeft = track.HasDownloadsLeft,
                LabelName = CleanupInvalidChars(track.LabelName),
                License = CleanupInvalidChars(track.License),
                LikesCount = track.LikesCount ?? 0,
                Permalink = CleanupInvalidChars(track.Permalink),
                PermalinkUrl = CleanupInvalidChars(track.PermalinkUrl?.ToString()),
                PlaybackCount = track.PlaybackCount ?? 0,
                Public = track.Public,
                PublisherMetadata_Artist = CleanupInvalidChars(track.PublisherMetadata?.Artist),
                PublisherMetadata_ContainsMusic = track.PublisherMetadata?.ContainsMusic ?? false,
                PublisherMetadata_Id = track.PublisherMetadata?.Id ?? 0,
                PublisherMetadata_Urn = CleanupInvalidChars(track.PublisherMetadata?.Urn),
                PurchaseTitle = CleanupInvalidChars(track.PurchaseTitle),
                PurchaseUrl = CleanupInvalidChars(track.PurchaseUrl),
                ReleaseDate = releaseDate,
                RepostsCount = track.RepostsCount ?? 0,
                Sharing = CleanupInvalidChars(track.Sharing),
                State = CleanupInvalidChars(track.State),
                Streamable = track.Streamable,
                TagList = CleanupInvalidChars(track.TagList),
                Uri = CleanupInvalidChars(track.Uri?.ToString()),
                ArtworkUrl = CleanupInvalidChars(track.ArtworkUrl?.ToString()),
                Visuals = CleanupInvalidChars(track.Visuals?.ToString()),
                WaveformUrl = CleanupInvalidChars(track.WaveformUrl),
                DisplayDate = track.DisplayDate.DateTime,
                MonetizationModel = CleanupInvalidChars(track.MonetizationModel),
                Policy = CleanupInvalidChars(track.Policy),
                Urn = CleanupInvalidChars(track.Urn),
                CreatedAt = track.CreatedAt.DateTime,
                LastModified = track.LastModified.DateTime
            }, transaction: base.Transaction);
    }
    
    public async Task UpsertPlaylistTrackAsync(long userId, long playlistId, long trackId, long trackOrder)
    {
        string query = @"
            INSERT INTO soundcloud_playlist_track (UserId, PlaylistId, TrackId, TrackOrder)
            VALUES (@userId, @playlistId, @trackId, @trackOrder)
            ON CONFLICT (UserId, PlaylistId, TrackId)
            DO NOTHING";

        await base.Connection
            .ExecuteAsync(query, param: new
            {
                userId, 
                playlistId, 
                trackId,
                trackOrder
            }, transaction: base.Transaction);
    }
    
    public async Task<int> GetPlaylistTrackCountAsync(long playlistId, long userId)
    {
        string query = @"SELECT count(*)
                         FROM soundcloud_playlist_track playlist
                         where playlist.PlaylistId = @playlistId and playlist.UserId = @userId
                         limit 1";

        return await base.Connection
            .ExecuteScalarAsync<int>(query,
                param: new
                {
                    playlistId,
                    userId
                }, transaction: base.Transaction);
    }

    public async Task<List<long>> GetAllUserIdsAsync()
    {
        string query = @"SELECT Id
                         FROM soundcloud_user
                         order by LastSyncTime asc";

        return (await base.Connection
            .QueryAsync<long>(query))
            .ToList();
    }

    private string CleanupInvalidChars(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }
        
        var invalidChars = new HashSet<char>
        {
            '\uFFFE',  // 0xFFFE - reversed BOM
            '\uFEFF',  // 0xFEFF - BOM / zero-width no-break space
            '\uFFFD',  // 0xFFFD - replacement character
            '\u0000',  // 0x0000 - null character
            ' '
        };
        if (invalidChars.Any(c => value.Contains(c)))
        {
            foreach(var c in invalidChars)
            {
                value = value.Replace(c.ToString(), string.Empty);
            }
        }
        return value;
    }
}