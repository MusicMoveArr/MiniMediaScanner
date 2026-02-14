using System.Text;
using FuzzySharp;
using MiniMediaScanner.Callbacks;
using MiniMediaScanner.Callbacks.Status;
using MiniMediaScanner.Enums;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models.Tidal;
using MiniMediaScanner.Repositories;
using Spectre.Console;

namespace MiniMediaScanner.Services;

public class TidalService
{
    public readonly int PreventUpdateWithinDays; 
    private readonly TidalAPICacheLayerService _tidalAPIService;
    private readonly string _connectionString;
    private readonly int _ignoreArtistAlbumAmount;

    public TidalService(string connectionString, 
        List<TidalTokenClientSecret> secretTokens,
        string countryCode, 
        string proxyFile, 
        string singleProxy, 
        string proxyMode,
        int preventUpdateWithinDays,
        int ignoreArtistAlbumAmount)
    {
        _ignoreArtistAlbumAmount = ignoreArtistAlbumAmount;
        _connectionString =  connectionString;
        this.PreventUpdateWithinDays = preventUpdateWithinDays;
        _tidalAPIService = new TidalAPICacheLayerService(secretTokens, countryCode, proxyFile, singleProxy, proxyMode);
    }
    
    public async Task UpdateArtistByNameAsync(string artistName,
        Action<UpdateTidalCallback>? callback = null)
    {
        var searchResult = await _tidalAPIService.SearchResultsArtistsAsync(artistName);

        if (searchResult?.Included?.Any() == true)
        {
            foreach (var artist in searchResult
                         ?.Included
                         ?.Where(artist => !string.IsNullOrWhiteSpace(artist?.Attributes?.Name))
                         ?.OrderByDescending(artist => Fuzz.Ratio(artistName, artist.Attributes.Name))
                         ?.Where(artist => Fuzz.Ratio(artistName, artist.Attributes.Name) > 80) ?? [])
            {
                if (_tidalAPIService.ProxyManagerService.ProxyMode == ProxyModeType.PerArtist)
                {
                    _tidalAPIService.ProxyManagerService.PickNextProxy();
                }
                
                try
                {
                    await UpdateArtistByIdAsync(int.Parse(artist.Id), callback);
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
    }

    public async Task UpdateArtistByIdAsync(int artistId,
        Action<UpdateTidalCallback>? callback = null)
    {
        UpdateTidalRepository _updateTidalRepository = new UpdateTidalRepository(_connectionString);
        await _updateTidalRepository.SetConnectionAsync();

        try
        {
            //get artist information
            var artistInfo = await InsertArtistInfoAsync(artistId, true);

            if (artistInfo == null)
            {
                await _updateTidalRepository.CommitAsync();
                return;
            }
            
            await PullMissingSimilarArtistsAsync(artistId, callback, artistInfo, _updateTidalRepository);
            await PullMissingSimilarAlbumsAsync(artistId, _updateTidalRepository, callback, artistInfo, 0);
            
            DateTime? lastSyncTime = await _updateTidalRepository.GetArtistLastSyncTimeAsync(artistId);
            if (lastSyncTime?.Year > 2000 && DateTime.Now.Subtract(lastSyncTime.Value).TotalDays < this.PreventUpdateWithinDays)
            {
                await _updateTidalRepository.CommitAsync();
                callback?.Invoke(new UpdateTidalCallback(artistId, UpdateTidalStatus.SkippedSyncedWithin));
                return;
            }
            
            //fetch all the albums available of the artist
            //by going through the next page cursor
            //populating the artist object
            if (!string.IsNullOrWhiteSpace(artistInfo.Data.RelationShips?.Albums?.Links?.Next))
            {
                string? nextPage = artistInfo.Data.RelationShips.Albums.Links.Next;
                while (!string.IsNullOrWhiteSpace(nextPage))
                {
                    var nextArtistInfo = await _tidalAPIService.GetArtistNextInfoByIdAsync(artistId, nextPage);

                    if (nextArtistInfo?.Data?.Count > 0)
                    {
                        artistInfo.Data.RelationShips.Albums.Data.AddRange(nextArtistInfo.Data);
                    }

                    if (nextArtistInfo?.Included?.Count > 0)
                    {
                        artistInfo.Included.AddRange(nextArtistInfo.Included);
                    }

                    nextPage = nextArtistInfo?.Links?.Next;
                }
            }

            //filter out only the albums (Included can contain artists, tracks etc)
            var albums = artistInfo.Included
                .Where(x => x.Type == "albums")
                .ToList();

            await PullMissingSimilarTracksAsync(artistId, _updateTidalRepository, callback, artistInfo, albums.Count);

            if (albums.Count >= _ignoreArtistAlbumAmount)
            {
                await _updateTidalRepository.CommitAsync();
                return;
            }

            int progress = 1;
            foreach (var album in albums)
            {
                callback?.Invoke(new UpdateTidalCallback(artistId, 
                    artistInfo.Data.Attributes.Name,
                    album.Attributes.Title,
                    albums.Count,
                    UpdateTidalStatus.Updating,
                    progress));
                
                string albumAvailability = string.Join(',', album.Attributes?.Availability ?? []);
                string albumMediaTags = string.Join(',', album.Attributes?.MediaTags ?? []);

                //insert album info
                await _updateTidalRepository.UpsertAlbumAsync(int.Parse(album.Id),
                    artistId,
                    album.Attributes.Title ?? string.Empty,
                    album.Attributes.BarcodeId ?? string.Empty,
                    album.Attributes.NumberOfVolumes,
                    album.Attributes.NumberOfItems,
                    album.Attributes.Duration ?? string.Empty,
                    album.Attributes.Explicit,
                    album.Attributes.ReleaseDate ?? string.Empty,
                    album.Attributes.Copyright?.Text ?? string.Empty,
                    album.Attributes.Popularity,
                    albumAvailability,
                    albumMediaTags);

                if (album?.Attributes?.ImageLinks?.Count > 0)
                {
                    foreach (var imageLink in album.Attributes.ImageLinks)
                    {
                        await _updateTidalRepository.UpsertAlbumImageLinkAsync(int.Parse(album.Id),
                            imageLink.Href,
                            imageLink.Meta.Width,
                            imageLink.Meta.Height);
                    }
                }
                if (album?.Attributes?.ExternalLinks?.Count > 0)
                {
                    foreach (var externalLink in album.Attributes.ExternalLinks)
                    {
                        await _updateTidalRepository.UpsertAlbumExternalLinkAsync(int.Parse(album.Id),
                            externalLink.Href,
                            externalLink.Meta.Type);
                    }
                }

                await ProcessSimilarAlbumAsync(int.Parse(album.Id), _updateTidalRepository);
                
                int dbTrackCount = await _updateTidalRepository.GetTidalAlbumTrackCountAsync(int.Parse(album.Id), artistId);
                if (dbTrackCount == album.Attributes.NumberOfItems)
                {
                    progress++;
                    continue;
                }
                
                //fetch all tracks of the album by using the page cursor
                //Tidal's API limit is 20 by default so only try if NumberOfItems is more than 20
                var tracks = await _tidalAPIService.GetTracksByAlbumIdAsync(int.Parse(album.Id));
                
                if (tracks.Data.Attributes.NumberOfItems >= 20)
                {
                    callback?.Invoke(new UpdateTidalCallback(artistId, 
                        artistInfo.Data.Attributes.Name,
                        album.Attributes.Title,
                        albums.Count,
                        UpdateTidalStatus.Updating,
                        progress,
                        $"Fetching all tracks... {tracks.Included.Count}"));
                    
                    string? nextPage = tracks.Data.RelationShips?.Items?.Links?.Next;
                    while (!string.IsNullOrWhiteSpace(nextPage))
                    {
                        var tempTracks = await _tidalAPIService.GetTracksNextByAlbumIdAsync(int.Parse(album.Id), nextPage);

                        if (tempTracks?.Included?.Count > 0)
                        {
                            tracks.Included.AddRange(tempTracks.Included);
                        }

                        if (tempTracks.Data?.Count > 0)
                        {
                            tracks.Data
                                ?.RelationShips
                                ?.Items
                                ?.Data
                                ?.AddRange(tempTracks.Data);
                        }
                        nextPage = tempTracks?.Links?.Next;
                    }
                }

                //grab all the artists associated with the track (feat. etc)
                //limit is 20 I think again here
                for (int trackOffset = 0;; trackOffset += 20)
                {
                    int totalTracks = tracks.Included
                        .Count(t => t.Type == "tracks");
                    
                    var trackids = tracks.Included
                        .Where(t => t.Type == "tracks")
                        .Select(t => int.Parse(t.Id))
                        .Skip(trackOffset)
                        .Take(20)
                        .ToArray();

                    if (trackids.Length == 0)
                    {
                        break;
                    }
                    
                    callback?.Invoke(new UpdateTidalCallback(artistId, 
                        artistInfo.Data.Attributes.Name,
                        album.Attributes.Title,
                        albums.Count,
                        UpdateTidalStatus.Updating,
                        progress,
                        $"Checking associated artists of tracks, {trackOffset} of {totalTracks} processed"));

                    var trackArtists = await _tidalAPIService.GetTrackArtistsByTrackIdAsync(trackids);

                    if (trackArtists == null)
                    {
                        break;
                    }

                    foreach (var track in trackArtists.Data
                                 .Where(t => t.Type == "tracks")
                                 .Where(t => t?.RelationShips?.Artists?.Data?.Count > 0))
                    {
                        foreach (var trackArtist in track.RelationShips.Artists.Data)
                        {
                            await _updateTidalRepository.UpsertTrackArtistIdAsync(int.Parse(track.Id), int.Parse(trackArtist.Id));
                            await ProcessSimilarArtistAsync(int.Parse(trackArtist.Id), callback, artistInfo, _updateTidalRepository);
                        }
                    }
                    
                    //insert all the artists, skip the ones already updated recently
                    int associatedArtistInserts = 0;
                    int associatedArtistCount = trackArtists.Included.Count(t => t.Type == "artists");
                    foreach (var artist in trackArtists.Included.Where(t => t.Type == "artists"))
                    {
                        int trackArtistId = int.Parse(artist.Id);
                        
                        callback?.Invoke(new UpdateTidalCallback(artistId, 
                            artistInfo.Data.Attributes.Name,
                            album.Attributes.Title,
                            albums.Count,
                            UpdateTidalStatus.Updating,
                            progress,
                            $"Inserting all associated artists, {associatedArtistInserts++} of {associatedArtistCount} processed"));

                        await InsertArtistInfoAsync(trackArtistId);
                    }
                }

                foreach (var provider in tracks.Included
                             .Where(t => t.Type == "providers"))
                {
                    await _updateTidalRepository.UpsertProviderAsync(int.Parse(provider.Id), provider.Attributes.Name);

                    //not sure if this is correct, wasn't documented, joining Track to Provider
                    foreach (var track in tracks.Included
                                 .Where(t => t.Type == "tracks"))
                    {
                        await _updateTidalRepository.UpsertTrackProviderAsync(int.Parse(track.Id), int.Parse(provider.Id));
                    }
                }

                int tracksProcessed = 0;
                int tracksToProcess = tracks.Included.Count(t => t.Type == "tracks");
                foreach (var track in tracks.Included
                             .Where(t => t.Type == "tracks"))
                {
                    var trackNumber = tracks.Data
                        ?.RelationShips
                        ?.Items
                        ?.Data
                        ?.FirstOrDefault(x => x.Id == track.Id);
                    
                    callback?.Invoke(new UpdateTidalCallback(artistId, 
                        artistInfo.Data.Attributes.Name,
                        album.Attributes.Title,
                        albums.Count,
                        UpdateTidalStatus.Updating,
                        progress,
                        $"Processing tracks {tracksProcessed++} of {tracksToProcess} processed"));
                    
                    if (trackNumber == null)
                    {
                        continue;
                    }
                    
                    //not always is our own ArtistId added to the track_artist table
                    //for some reason Tidal does always give back our own ArtistId in the artists list
                    await _updateTidalRepository.UpsertTrackArtistIdAsync(int.Parse(track.Id), artistId);
                    
                    string trackAvailability = string.Join(',', track?.Attributes?.Availability ?? []);
                    string trackMediaTags = string.Join(',', track?.Attributes?.MediaTags ?? []);

                    if (track?.Attributes?.ExternalLinks?.Any() == true)
                    {
                        foreach (var externalLink in track.Attributes.ExternalLinks)
                        {
                            await _updateTidalRepository.UpsertTrackExternalLinkAsync(int.Parse(track.Id),
                                externalLink.Href,
                                externalLink.Meta.Type);
                        }
                    }

                    if (track?.Attributes?.ImageLinks?.Any() == true)
                    {
                        foreach (var imageLink in track.Attributes.ImageLinks)
                        {
                            await _updateTidalRepository.UpsertTrackImageLinkAsync(int.Parse(track.Id),
                                imageLink.Href,
                                imageLink.Meta.Width,
                                imageLink.Meta.Height);
                        }
                    }

                    await _updateTidalRepository.UpsertTrackAsync(int.Parse(track.Id),
                        int.Parse(album.Id),
                        track.Attributes.Title ?? string.Empty,
                        track.Attributes.ISRC ?? string.Empty,
                        track.Attributes.Duration ?? string.Empty,
                        track.Attributes.Copyright?.Text ?? string.Empty,
                        track.Attributes.Explicit,
                        track.Attributes.Popularity,
                        trackAvailability,
                        trackMediaTags,
                        trackNumber.Meta.VolumeNumber,
                        trackNumber.Meta.TrackNumber,
                        track.Attributes.Version ?? string.Empty);

                    await ProcessSimilarTrackAsync(int.Parse(track.Id), _updateTidalRepository);
                }
                progress++;
            }

            await _updateTidalRepository.SetArtistLastSyncTimeAsync(artistId);
            await _updateTidalRepository.CommitAsync();
        }
        catch (Npgsql.NpgsqlException e)
        {
            await _updateTidalRepository.RollbackAsync();
            Console.WriteLine($"{e.Message}, {e.StackTrace}");
            throw;
        }
        catch (Exception e)
        {
            await _updateTidalRepository.RollbackAsync();
            Console.WriteLine($"{e.Message}, {e.StackTrace}");
        }
    }

    private async Task PullMissingSimilarTracksAsync(
        int artistId, 
        UpdateTidalRepository updateTidalRepository, 
        Action<UpdateTidalCallback>? callback,
        TidalSearchResponse artistInfo,
        int albumCount)
    {
        List<int> trackIdsMissingSimilar = await updateTidalRepository.GetMissingSimilarTrackIdsByArtistIdAsync(artistId);

        int repullProgress = 1;
        foreach(int trackId in trackIdsMissingSimilar)
        {
            await ProcessSimilarTrackAsync(trackId, updateTidalRepository);
                
            callback?.Invoke(new UpdateTidalCallback(artistId, 
                artistInfo.Data.Attributes.Name,
                string.Empty,
                albumCount,
                UpdateTidalStatus.Updating,
                0,
                $"Pulling missing similar tracks {repullProgress} of {trackIdsMissingSimilar.Count} processed"));
            repullProgress++;
        }
    }

    private async Task PullMissingSimilarAlbumsAsync(
        int artistId, 
        UpdateTidalRepository updateTidalRepository, 
        Action<UpdateTidalCallback>? callback,
        TidalSearchResponse artistInfo,
        int albumCount)
    {
        List<int> albumIdsMissingSimilar = await updateTidalRepository.GetMissingSimilarAlbumIdsByArtistIdAsync(artistId);

        int repullProgress = 1;
        foreach(int albumId in albumIdsMissingSimilar)
        {
            await ProcessSimilarAlbumAsync(albumId, updateTidalRepository);
                
            callback?.Invoke(new UpdateTidalCallback(artistId, 
                artistInfo.Data.Attributes.Name,
                string.Empty,
                albumCount,
                UpdateTidalStatus.Updating,
                0,
                $"Pulling missing similar albums {repullProgress} of {albumIdsMissingSimilar.Count} processed"));
            repullProgress++;
        }
    }

    private async Task PullMissingSimilarArtistsAsync(
        int artistId, 
        Action<UpdateTidalCallback>? callback,
        TidalSearchResponse artistInfo,
        UpdateTidalRepository updateTidalRepository)
    {
        List<int> artistIdsMissingSimilar = await updateTidalRepository.GetMissingSimilarArtistIdsByArtistIdAsync(artistId);
        
        foreach(int similarArtistId in artistIdsMissingSimilar)
        {
            await ProcessSimilarArtistAsync(similarArtistId, callback, artistInfo, updateTidalRepository);
        }
    }

    private async Task ProcessSimilarTrackAsync(int trackId, UpdateTidalRepository updateTidalRepository)
    {
        if (!await updateTidalRepository.HasSimilarTrackRecordsAsync(trackId))
        {
            var similarTracks = await _tidalAPIService.GetSimilarTracksByTrackIdAsync(trackId);

            foreach (var similarTrack in similarTracks?.Data ?? [])
            {
                string similarIsrc = similarTracks
                    ?.Included
                    ?.Where(x => x.Id == similarTrack.Id)
                    ?.Select(x => x.Attributes.ISRC)
                    ?.FirstOrDefault() ?? string.Empty;
                        
                await updateTidalRepository.UpsertSimilarTrackAsync(
                    trackId, 
                    int.Parse(similarTrack.Id), 
                    similarIsrc);
            }
        }
    }

    private async Task ProcessSimilarAlbumAsync(int albumId, UpdateTidalRepository updateTidalRepository)
    {
        if (!await updateTidalRepository.HasSimilarAlbumRecordsAsync(albumId))
        {
            var similarAlbums = await _tidalAPIService.GetSimilarAlbumsByAlbumIdAsync(albumId);

            foreach (var similarAlbum in similarAlbums?.Data ?? [])
            {
                await updateTidalRepository.UpsertSimilarAlbumAsync(
                    albumId, 
                    int.Parse(similarAlbum.Id));
            }
        }
    }

    private async Task ProcessSimilarArtistAsync(
        int artistId, 
        Action<UpdateTidalCallback>? callback,
        TidalSearchResponse artistInfo,
        UpdateTidalRepository updateTidalRepository)
    {
        if (!await updateTidalRepository.HasSimilarArtistRecordsAsync(artistId))
        {
            var similarArtists = await _tidalAPIService.GetSimilarArtistsByArtistIdAsync(artistId);
            int repullProgress = 1;
            
            foreach (var similarArtist in similarArtists?.Data ?? [])
            {
                int similarArtistId = int.Parse(similarArtist.Id);
                await InsertArtistInfoAsync(similarArtistId);
                
                await updateTidalRepository.UpsertSimilarArtistAsync(
                    artistId, 
                    similarArtistId);
                
                callback?.Invoke(new UpdateTidalCallback(artistId, 
                    artistInfo.Data.Attributes.Name,
                    string.Empty,
                    0,
                    UpdateTidalStatus.Updating,
                    0,
                    $"Pulling missing similar artists {repullProgress} of {similarArtists.Data.Count} processed"));
                repullProgress++;
            }
        }
    }

    private async Task<TidalSearchResponse?> InsertArtistInfoAsync(int artistId, bool ignorePeventCheck = false)
    {
        UpdateTidalRepository _updateTidalRepository = new UpdateTidalRepository(_connectionString);
        await _updateTidalRepository.SetConnectionAsync();
        if (!ignorePeventCheck)
        {
            DateTime? lastSyncTime = await _updateTidalRepository.GetArtistLastSyncTimeAsync(artistId);
            if (lastSyncTime?.Year > 2000 && DateTime.Now.Subtract(lastSyncTime.Value).TotalDays < PreventUpdateWithinDays)
            {
                await _updateTidalRepository.CommitAsync();
                return null;
            }
        }
        
        var artistInfo = await _tidalAPIService.GetArtistInfoByIdAsync(artistId);

        if (artistInfo == null || 
            artistInfo?.Data == null || 
            artistInfo?.Included == null)
        {
            await _updateTidalRepository.CommitAsync();
            return null;
        }
        
        await _updateTidalRepository.UpsertArtistAsync(artistId,
            artistInfo.Data.Attributes.Name,
            artistInfo.Data.Attributes.Popularity);

        foreach (var externalLink in artistInfo?.Data?.Attributes?.ExternalLinks ?? [])
        {
            await _updateTidalRepository.UpsertArtistExternalLinkAsync(artistId, externalLink.Href, externalLink.Meta.Type);
        }

        foreach (var imageLink in artistInfo?.Data?.Attributes?.ImageLinks ?? [])
        {
            await _updateTidalRepository.UpsertArtistImageLinkAsync(artistId,
                imageLink.Href,
                imageLink.Meta.Width,
                imageLink.Meta.Height);
        }

        await _updateTidalRepository.CommitAsync();
        return artistInfo;
    }
}