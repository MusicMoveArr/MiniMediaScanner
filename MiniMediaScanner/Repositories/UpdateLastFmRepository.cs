using Dapper;

namespace MiniMediaScanner.Repositories;

public class UpdateLastFmRepository : BaseUpdateRepository
{
    public UpdateLastFmRepository(string connectionString)
        : base(connectionString)
    {
        
    }
    
    public async Task<DateTime?> GetArtistLastSyncTimeAsync(string artistName)
    {
        string query = @"SELECT lastsynctime FROM lastfm_artist WHERE Name = @artistName";

        return await base.Connection
            .ExecuteScalarAsync<DateTime>(query, new
            {
                artistName
            }, transaction: base.Transaction);
    }
    
    public async Task<Guid?> GetArtistIdByNameAsync(string artistName)
    {
        string query = @"SELECT ArtistId FROM lastfm_artist WHERE Name = @artistName";

        return await base.Connection
            .ExecuteScalarAsync<Guid>(query, new
            {
                artistName
            }, transaction: base.Transaction);
    }
    
    public async Task<DateTime?> SetArtistLastSyncTimeAsync(Guid artistId)
    {
        string query = @"UPDATE lastfm_artist SET lastsynctime = @lastsynctime WHERE ArtistId = @artistId";

        return await base.Connection
            .ExecuteScalarAsync<DateTime>(query, new
            {
                artistId,
                lastsynctime = DateTime.Now
            }, transaction: base.Transaction);
    }
    
    public async Task<Guid?> GetTrackIdByNameAsync(string artistName, string albumName, string title)
    {
        string query = @"SELECT abt.TrackId 
                         FROM lastfm_artist a
                         join lastfm_album ab on ab.ArtistId = a.ArtistId
                         join lastfm_album_track abt on abt.AlbumId = ab.AlbumId
                         WHERE lower(a.Name) = lower(@artistName)
                         AND (lower(ab.Name) = lower(@albumName) or @albumName is null)
                         and lower(abt.Name) = lower(@title)";

        return await base.Connection
            .ExecuteScalarAsync<Guid>(query, new
            {
                artistName,
                albumName,
                title
            }, transaction: base.Transaction);
    }
    
    public async Task<Guid> UpsertArtistAsync(
        string lastFmId, 
        string name,
        bool onTour,
        int statsListeners,
        Guid? musicBrainzId,
        string bioContent,
        string bioSummary,
        int bioYearFormed,
        DateTime bioPublished,
        string uri)
    {
        string query = @"
            INSERT INTO lastfm_artist (ArtistId, 
                                  LastFmId,
                                  Name, 
                                  OnTour, 
                                  StatsListeners, 
                                  MusicBrainzId, 
                                  BioContent, 
                                  BioSummary, 
                                  BioYearFormed, 
                                  BioPublished, 
                                  Uri, 
                                  lastsynctime)
            VALUES (@id, @lastFmId, @name, @onTour, @statsListeners, @musicBrainzId,
                    @bioContent, @bioSummary, @bioYearFormed, @bioPublished, @uri, @lastsynctime)
            ON CONFLICT (Name)
            DO UPDATE SET
                LastFmId = EXCLUDED.LastFmId,
                OnTour = EXCLUDED.OnTour,
                StatsListeners = EXCLUDED.StatsListeners,
                MusicBrainzId = EXCLUDED.MusicBrainzId,
                BioContent = EXCLUDED.BioContent,
                BioSummary = EXCLUDED.BioSummary,
                BioYearFormed = EXCLUDED.BioYearFormed,
                BioPublished = EXCLUDED.BioPublished,
                Uri = EXCLUDED.Uri
            RETURNING ArtistId";

        return await base.Connection
            .ExecuteScalarAsync<Guid>(query, param: new
            {
                id = Guid.NewGuid(),
                lastFmId,
                name,
                onTour,
                statsListeners,
                musicBrainzId,
                bioContent,
                bioSummary,
                bioYearFormed,
                bioPublished,
                uri,
                lastsynctime = new DateTime(2000, 1, 1)
            }, transaction: base.Transaction);
    }
    
    public async Task UpsertArtistImageAsync(Guid artistId, string uri)
    {
        string query = @"
            INSERT INTO lastfm_artist_image (ArtistId, Uri)
            VALUES (@artistId, @uri)
            ON CONFLICT (ArtistId, Uri)
            DO NOTHING";

        await base.Connection
            .ExecuteAsync(query, param: new
            {
                artistId,
                uri
            }, transaction: base.Transaction);
    }
    public async Task UpsertArtistTagAsync(
        Guid artistId, 
        string name,
        int count,
        int reach, 
        string relatedTo,
        bool streamable,
        string uri)
    {
        string query = @"
            INSERT INTO lastfm_artist_tag (ArtistId, Name, Count, Reach, RelatedTo, Streamable, Uri)
            VALUES (@artistId, @name, @count, @reach, @relatedTo, @streamable, @uri)
            ON CONFLICT (ArtistId, Name)
            DO NOTHING";

        await base.Connection
            .ExecuteAsync(query, param: new
            {
                artistId,
                name,
                count,
                reach,
                relatedTo,
                streamable,
                uri
            }, transaction: base.Transaction);
    }
    
    public async Task<Guid> UpsertAlbumAsync(
        Guid artistId,
        string lastfmAlbumId,
        Guid? musicBrainzId,
        int listenerCount,
        string name,
        int playCount,
        DateTime? releaseDateUtc,
        string url)
    {
        string query = @"
            INSERT INTO lastfm_album (AlbumId, 
                                  ArtistId, 
                                  LastFmAlbumId, 
                                  MusicBrainzId, 
                                  ListenerCount, 
                                  Name, 
                                  PlayCount, 
                                  ReleaseDateUtc,
                                  Url)
            VALUES (@albumId, @artistId, @lastfmAlbumId, @musicBrainzId, 
                    @listenerCount, @name, @playCount, 
                    @releaseDateUtc, @url)
            ON CONFLICT (ArtistId, Name)
            DO UPDATE SET
                LastFmAlbumId = EXCLUDED.LastFmAlbumId,
                MusicBrainzId = EXCLUDED.MusicBrainzId,
                ListenerCount = EXCLUDED.ListenerCount,
                Name = EXCLUDED.Name,
                PlayCount = EXCLUDED.PlayCount,
                ReleaseDateUtc = EXCLUDED.ReleaseDateUtc
            RETURNING AlbumId";

        return await base.Connection
            .QueryFirstAsync<Guid>(query, param: new
            {
                artistId,
                albumId = Guid.NewGuid(),
                lastfmAlbumId,
                musicBrainzId,
                listenerCount,
                name,
                playCount,
                releaseDateUtc,
                url
            }, transaction: base.Transaction);
    }
    public async Task UpsertTrackTagAsync(
        Guid trackId, 
        string name,
        int count,
        int reach, 
        string relatedTo,
        bool streamable,
        string uri)
    {
        string query = @"
            INSERT INTO lastfm_album_track_tag (TrackId, Name, Count, Reach, RelatedTo, Streamable, Uri)
            VALUES (@trackId, @name, @count, @reach, @relatedTo, @streamable, @uri)
            ON CONFLICT (TrackId, Name)
            DO NOTHING";

        await base.Connection
            .ExecuteAsync(query, param: new
            {
                trackId,
                name,
                count,
                reach,
                relatedTo,
                streamable,
                uri
            }, transaction: base.Transaction);
    }
    public async Task UpsertAlbumTagAsync(
        Guid albumId, 
        string name,
        int count,
        int reach, 
        string relatedTo,
        bool streamable,
        string uri)
    {
        string query = @"
            INSERT INTO lastfm_album_tag (AlbumId, Name, Count, Reach, RelatedTo, Streamable, Uri)
            VALUES (@albumId, @name, @count, @reach, @relatedTo, @streamable, @uri)
            ON CONFLICT (AlbumId, Name)
            DO NOTHING";

        await base.Connection
            .ExecuteAsync(query, param: new
            {
                albumId,
                name,
                count,
                reach,
                relatedTo,
                streamable,
                uri
            }, transaction: base.Transaction);
    }
    
    public async Task UpsertAlbumImageAsync(Guid albumId, string uri)
    {
        string query = @"
            INSERT INTO lastfm_album_image (AlbumId, uri)
            VALUES (@albumId, @uri)
            ON CONFLICT (AlbumId, Uri)
            DO NOTHING";

        await base.Connection
            .ExecuteAsync(query, param: new
            {
                albumId,
                uri
            }, transaction: base.Transaction);
    }
    
    public async Task<Guid> UpsertTrackAsync(
        string lastFmTrackId,
        Guid albumId,
        string name,
        int rank,
        int duration,
        Guid? musicBrainzId,
        int listenerCount,
        int playCount,
        string url)
    {
        string query = @"
            INSERT INTO lastfm_album_track (TrackId, 
                                  AlbumId, 
                                  LastFmTrackId,
                                  Name, 
                                  Rank, 
                                  Duration, 
                                  MusicBrainzId, 
                                  ListenerCount, 
                                  PlayCount,
                                  Url)
            VALUES (@trackId, @albumId, @lastFmTrackId, @name, 
                    @rank, @duration, @musicBrainzId, @listenerCount, 
                    @playCount, @url)
            ON CONFLICT (AlbumId, Name, Rank)
            DO UPDATE SET
                LastFmTrackId = EXCLUDED.LastFmTrackId,
                Rank = EXCLUDED.Rank,
                Duration = EXCLUDED.Duration,
                MusicBrainzId = EXCLUDED.MusicBrainzId,
                ListenerCount = EXCLUDED.ListenerCount,
                PlayCount = EXCLUDED.PlayCount,
                Url = EXCLUDED.Url
            RETURNING TrackId";

        return await base.Connection
            .QueryFirstAsync<Guid>(query, param: new
            {
                trackId = Guid.NewGuid(),
                albumId,
                lastFmTrackId,
                name,
                rank,
                duration,
                musicBrainzId,
                listenerCount,
                playCount, 
                url
            }, transaction: base.Transaction);
    }
    
    public async Task UpsertSimilarTrackAsync(Guid trackId, Guid similarTrackId)
    {
        string query = @"
            INSERT INTO lastfm_album_track_similar (TrackId, SimilarTrackId)
            VALUES (@trackId, @similarTrackId)
            ON CONFLICT (TrackId, SimilarTrackId)
            DO NOTHING";

        await base.Connection
            .ExecuteAsync(query, param: new
            {
                trackId,
                similarTrackId
            }, transaction: base.Transaction);
    }
    
    public async Task UpsertSimilarArtistAsync(Guid artistId, Guid similarArtistId)
    {
        string query = @"
            INSERT INTO lastfm_artist_similar (ArtistId, SimilarArtistId)
            VALUES (@artistId, @similarArtistId)
            ON CONFLICT (ArtistId, SimilarArtistId)
            DO NOTHING";

        await base.Connection
            .ExecuteAsync(query, param: new
            {
                artistId,
                similarArtistId
            }, transaction: base.Transaction);
    }
}