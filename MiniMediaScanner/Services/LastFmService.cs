using FuzzySharp;
using IF.Lastfm.Core.Api;
using IF.Lastfm.Core.Objects;
using MiniMediaScanner.Callbacks;
using MiniMediaScanner.Callbacks.Status;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Repositories;

namespace MiniMediaScanner.Services;

public class LastFmService
{
    private const string NoArtCoverImageUrl = "https://lastfm.freetls.fastly.net/i/u/300x300/2a96cbd8b46e442fc41c2b86b821562f.png";
    public readonly int PreventUpdateWithinDays; 
    private readonly string _connectionString;
    private readonly LastfmClient _client;
    private const int PagingSize = 100;
    private readonly int _maxAlbumCountToPull;

    public LastFmService(string connectionString, 
        string lastfmApiKey,
        string lastfmSharedSecret,
        int preventUpdateWithinDays, 
        int maxAlbumCountToPull)
    {
        _maxAlbumCountToPull = maxAlbumCountToPull;
        _connectionString =  connectionString;
        this.PreventUpdateWithinDays = preventUpdateWithinDays;
        _client = new LastfmClient(lastfmApiKey, lastfmSharedSecret);
    }
    
    public async Task SearchArtistByNameAsync(string artistName,
        Action<UpdateLastFmCallback>? callback = null)
    {
        var searchResult = await _client.Artist.SearchAsync(artistName);

        foreach (var artist in searchResult
                     ?.Where(artist => !string.IsNullOrWhiteSpace(artist?.Name))
                     ?.OrderByDescending(artist => Fuzz.Ratio(artistName, artist.Name))
                     ?.Where(artist => Fuzz.Ratio(artistName, artist.Name) > 80) ?? [])
        {
            try
            {
                await UpdateArtistByNameAsync(artist.Name, callback);
            }
            catch (Npgsql.NpgsqlException e)
            {
                Console.WriteLine($"{e.Message}, {e.StackTrace}");
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message}, {e.StackTrace}");
            }
        }
    }

    public async Task UpdateArtistByNameAsync(string artistName,
        Action<UpdateLastFmCallback>? callback = null)
    {
        UpdateLastFmRepository updateLastFmRepository = new UpdateLastFmRepository(_connectionString);
        await updateLastFmRepository.SetConnectionAsync();

        try
        {
            //get artist information
            Guid? artistId = await InsertArtistInfoAsync(artistName, true);

            if (!GuidHelper.GuidHasValue(artistId))
            {
                await updateLastFmRepository.CommitAsync();
                return;
            }
            
            DateTime? lastSyncTime = await updateLastFmRepository.GetArtistLastSyncTimeAsync(artistName);
            if (lastSyncTime?.Year > 2000 && DateTime.Now.Subtract(lastSyncTime.Value).TotalDays < this.PreventUpdateWithinDays)
            {
                await updateLastFmRepository.CommitAsync();
                callback?.Invoke(new UpdateLastFmCallback(artistName, UpdateLastFmStatus.SkippedSyncedWithin));
                return;
            }

            foreach (var similarArtist in await _client.Artist.GetSimilarAsync(artistName))
            {
                Guid? tempArtistId = await InsertArtistInfoAsync(similarArtist.Name);
                if (GuidHelper.GuidHasValue(tempArtistId))
                {
                    await updateLastFmRepository.UpsertSimilarArtistAsync(artistId.Value, tempArtistId!.Value);
                }
            }
            
            var readonlyAlbums = await _client.Artist.GetTopAlbumsAsync(artistName, itemsPerPage: PagingSize);
            var albums = readonlyAlbums.Content.ToList();
            
            if (albums.Count() == PagingSize)
            {
                for (int page = 2; page < PagingSize * 10 && albums.Count < _maxAlbumCountToPull; page++)
                {
                    var tempAalbums = await _client.Artist.GetTopAlbumsAsync(artistName, itemsPerPage: PagingSize, page: page);
                    albums.AddRange(tempAalbums.Content);
                    
                    if (tempAalbums.Count() != PagingSize)
                    {
                        break;
                    }
                }
            }

            int progress = 1;
            foreach (var album in albums)
            {
                callback?.Invoke(new UpdateLastFmCallback(
                    artistName, 
                    album.Name,
                    albums.Count,
                    UpdateLastFmStatus.Updating,
                    progress));
                
                Guid albumId = await updateLastFmRepository.UpsertAlbumAsync(
                    artistId.Value,
                    album.Id,
                    Guid.TryParse(album.Mbid, out Guid result) ? result : null,
                    album.ListenerCount ?? 0,
                    album.Name,
                    album.PlayCount ?? 0,
                    album.ReleaseDateUtc?.DateTime,
                    album.Url.AbsoluteUri);

                string? albumImage = album.Images.Largest?.AbsoluteUri ?? 
                                    string.Empty;

                if (!string.IsNullOrWhiteSpace(albumImage) &&
                    albumImage != NoArtCoverImageUrl)
                {
                    await updateLastFmRepository.UpsertAlbumImageAsync(albumId, albumImage);
                }
                
                foreach (var tag in await _client.Album.GetTopTagsAsync(album.ArtistName, album.Name) ?? [])
                {
                    await updateLastFmRepository.UpsertAlbumTagAsync(
                        albumId, 
                        tag.Name,
                        tag.Count ?? 0,
                        tag.Reach ?? 0,
                        tag.RelatedTo ?? string.Empty,
                        tag.Streamable ?? false,
                        tag.Url.AbsoluteUri);
                }

                LastTrack[] tracks = null;
                
                try
                {
                    //this part crashes from the library, not sure how to fix it myself
                    tracks = (await _client.Album.GetInfoAsync(album.ArtistName, album.Name))?.Content?.Tracks?.ToArray() ?? [];
                }
                catch (Exception e)
                {
                    
                }

                foreach(var track in tracks ?? [])
                {
                    Guid trackId = await updateLastFmRepository.UpsertTrackAsync(
                        track.Id,
                        albumId,
                        track.Name,
                        track.Rank ?? 0,
                        (int)(track.Duration?.TotalSeconds ?? 0),
                        Guid.TryParse(track.Mbid, out Guid trackResult) ? trackResult : null,
                        track.ListenerCount ?? 0,
                        track.PlayCount ?? 0,
                        track.Url.AbsoluteUri);

                    foreach(var similarTrack in await _client.Track.GetSimilarAsync(track.Name, track.ArtistName))
                    {
                        Guid? simTrackId = await updateLastFmRepository.GetTrackIdByNameAsync(
                            similarTrack.ArtistName,
                            similarTrack.AlbumName, 
                            similarTrack.Name);
                        
                        if (GuidHelper.GuidHasValue(simTrackId))
                        {
                            await updateLastFmRepository.UpsertSimilarTrackAsync(trackId, simTrackId!.Value);
                        }
                    }

                    foreach (var tag in track.TopTags ?? [])
                    {
                        await updateLastFmRepository.UpsertTrackTagAsync(
                            trackId, 
                            tag.Name,
                            tag.Count ?? 0,
                            tag.Reach ?? 0,
                            tag.RelatedTo ?? string.Empty,
                            tag.Streamable ?? false,
                            tag.Url.AbsoluteUri);
                    }
                }

                progress++;
            }

            await updateLastFmRepository.SetArtistLastSyncTimeAsync(artistId.Value);
            await updateLastFmRepository.CommitAsync();
        }
        catch (Npgsql.NpgsqlException e)
        {
            await updateLastFmRepository.RollbackAsync();
            Console.WriteLine($"{e.Message}, {e.StackTrace}");
            throw;
        }
        catch (Exception e)
        {
            await updateLastFmRepository.RollbackAsync();
            Console.WriteLine($"{e.Message}, {e.StackTrace}");
        }
    }

    private async Task<Guid?> InsertArtistInfoAsync(string artistName, bool ignorePeventCheck = false)
    {
        UpdateLastFmRepository updateLastFmRepository = new UpdateLastFmRepository(_connectionString);
        await updateLastFmRepository.SetConnectionAsync();
        if (!ignorePeventCheck)
        {
            DateTime? lastSyncTime = await updateLastFmRepository.GetArtistLastSyncTimeAsync(artistName);
            if (lastSyncTime?.Year > 2000 && DateTime.Now.Subtract(lastSyncTime.Value).TotalDays < PreventUpdateWithinDays)
            {
                Guid? tempArtistId = await updateLastFmRepository.GetArtistIdByNameAsync(artistName);
                await updateLastFmRepository.CommitAsync();
                return tempArtistId;
            }
        }
        
        var artistInfo = await _client.Artist.GetInfoAsync(artistName);

        if (artistInfo?.Success == false)
        {
            await updateLastFmRepository.CommitAsync();
            return null;
        }
        
        Guid artistId = await updateLastFmRepository.UpsertArtistAsync(
            artistInfo.Content.Id,
            artistInfo.Content.Name,
            artistInfo.Content.OnTour,
            artistInfo.Content.Stats.Listeners,
            Guid.TryParse(artistInfo.Content.Mbid, out Guid result) ? result : null,
            artistInfo.Content.Bio.Content,
            artistInfo.Content.Bio.Summary,
            artistInfo.Content.Bio.YearFormed,
            artistInfo.Content.Bio.Published.DateTime,
            artistInfo.Content.Url.AbsoluteUri);

        if(!string.IsNullOrWhiteSpace(artistInfo.Content.MainImage?.Largest?.AbsoluteUri) &&
           artistInfo.Content.MainImage?.Largest?.AbsoluteUri != NoArtCoverImageUrl)
        {
            await updateLastFmRepository.UpsertArtistImageAsync(artistId, artistInfo.Content.MainImage?.Largest?.AbsoluteUri);
        }
        foreach (var tag in artistInfo.Content.Tags)
        {
            await updateLastFmRepository.UpsertArtistTagAsync(
                artistId, 
                tag.Name,
                tag.Count ?? 0,
                tag.Reach ?? 0,
                tag.RelatedTo ?? string.Empty,
                tag.Streamable ?? false,
                tag.Url.AbsoluteUri);
        }

        await updateLastFmRepository.CommitAsync();
        return artistId;
    }
}