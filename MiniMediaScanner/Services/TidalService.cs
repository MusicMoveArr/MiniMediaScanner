using System.Diagnostics;
using FuzzySharp;
using MiniMediaScanner.Callbacks;
using MiniMediaScanner.Callbacks.Status;
using MiniMediaScanner.Enums;
using MiniMediaScanner.Models.Tidal;
using MiniMediaScanner.Repositories;
using SmartFormat.Utilities;

namespace MiniMediaScanner.Services;

public class TidalService
{
    private const int PreventUpdateWithinDays = 7; 
    private readonly TidalAPICacheLayerService _tidalAPIService;
    private readonly TidalRepository _tidalRepository;

    public TidalService(string connectionString, 
        string clientId, 
        string clientSecret, 
        string countryCode, 
        string proxyFile, 
        string singleProxy, 
        string proxyMode)
    {
        _tidalRepository = new TidalRepository(connectionString);
        _tidalAPIService = new TidalAPICacheLayerService(clientId, clientSecret, countryCode, proxyFile, singleProxy, proxyMode);
    }

    private async Task RefreshTokenAsync()
    {
        if (string.IsNullOrWhiteSpace(_tidalAPIService.AuthenticationResponse?.AccessToken) ||
            (_tidalAPIService.AuthenticationResponse?.ExpiresIn > 0 &&
             DateTime.Now > _tidalAPIService.AuthenticationResponse?.ExpiresAt))
        {
            await _tidalAPIService.AuthenticateAsync();
        }
    }
    
    public async Task UpdateArtistByNameAsync(string artistName,
        Action<UpdateTidalCallback>? callback = null)
    {
        await RefreshTokenAsync();
        var searchResult = await _tidalAPIService.SearchResultsArtistsAsync(artistName);

        if (searchResult?.Included?.Any() == true)
        {
            foreach (var artist in searchResult
                         ?.Included
                         ?.Where(artist => !string.IsNullOrWhiteSpace(artist?.Attributes?.Name))
                         ?.Where(artist => Fuzz.Ratio(artistName, artist.Attributes.Name) > 80))
            {
                if (_tidalAPIService.ProxyManagerService.ProxyMode == ProxyModeType.PerArtist)
                {
                    _tidalAPIService.ProxyManagerService.PickNextProxy();
                }
                
                try
                {
                    await UpdateArtistByIdAsync(int.Parse(artist.Id), callback);
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
        await RefreshTokenAsync();

        DateTime? lastSyncTime = await _tidalRepository.GetArtistLastSyncTimeAsync(artistId);
        if (lastSyncTime?.Year > 2000 && DateTime.Now.Subtract(lastSyncTime.Value).TotalDays < PreventUpdateWithinDays)
        {
            callback?.Invoke(new UpdateTidalCallback(artistId, UpdateTidalStatus.SkippedSyncedWithin));
            return;
        }

        //get artist information
        var artistInfo = await InsertArtistInfoAsync(artistId, true);

        if (artistInfo == null)
        {
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
            await _tidalRepository.UpsertAlbumAsync(int.Parse(album.Id),
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
                    await _tidalRepository.UpsertAlbumImageLinkAsync(int.Parse(album.Id),
                        imageLink.Href,
                        imageLink.Meta.Width,
                        imageLink.Meta.Height);
                }
            }
            if (album?.Attributes?.ExternalLinks?.Count > 0)
            {
                foreach (var externalLink in album.Attributes.ExternalLinks)
                {
                    await _tidalRepository.UpsertAlbumExternalLinkAsync(int.Parse(album.Id),
                        externalLink.Href,
                        externalLink.Meta.Type);
                }
            }
            
            int dbTrackCount = await _tidalRepository.GetTidalAlbumTrackCountAsync(int.Parse(album.Id), artistId);
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
                        _tidalRepository.UpsertTrackArtistIdAsync(int.Parse(track.Id), int.Parse(trackArtist.Id));
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
                await _tidalRepository.UpsertProviderAsync(int.Parse(provider.Id), provider.Attributes.Name);

                //not sure if this is correct, wasn't documented, joining Track to Provider
                foreach (var track in tracks.Included
                             .Where(t => t.Type == "tracks"))
                {
                    await _tidalRepository.UpsertTrackProviderAsync(int.Parse(track.Id), int.Parse(provider.Id));
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
                _tidalRepository.UpsertTrackArtistIdAsync(int.Parse(track.Id), artistId);
                
                string trackAvailability = string.Join(',', track?.Attributes?.Availability ?? []);
                string trackMediaTags = string.Join(',', track?.Attributes?.MediaTags ?? []);

                if (track?.Attributes?.ExternalLinks?.Any() == true)
                {
                    foreach (var externalLink in track.Attributes.ExternalLinks)
                    {
                        await _tidalRepository.UpsertTrackExternalLinkAsync(int.Parse(track.Id),
                            externalLink.Href,
                            externalLink.Meta.Type);
                    }
                }

                if (track?.Attributes?.ImageLinks?.Any() == true)
                {
                    foreach (var imageLink in track.Attributes.ImageLinks)
                    {
                        await _tidalRepository.UpsertTrackImageLinkAsync(int.Parse(track.Id),
                            imageLink.Href,
                            imageLink.Meta.Width,
                            imageLink.Meta.Height);
                    }
                }

                await _tidalRepository.UpsertTrackAsync(int.Parse(track.Id),
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
            }

            progress++;
        }

        await _tidalRepository.SetArtistLastSyncTimeAsync(artistId);
    }

    private async Task<TidalSearchResponse?> InsertArtistInfoAsync(int artistId, bool ignorePeventCheck = false)
    {
        if (!ignorePeventCheck)
        {
            DateTime? lastSyncTime = await _tidalRepository.GetArtistLastSyncTimeAsync(artistId);
            if (lastSyncTime?.Year > 2000 && DateTime.Now.Subtract(lastSyncTime.Value).TotalDays < PreventUpdateWithinDays)
            {
                return null;
            }
        }
        
        var artistInfo = await _tidalAPIService.GetArtistInfoByIdAsync(artistId);

        if (artistInfo == null || 
            artistInfo?.Data == null || 
            artistInfo?.Included == null)
        {
            return null;
        }
        
        await _tidalRepository.UpsertArtistAsync(artistId,
            artistInfo.Data.Attributes.Name,
            artistInfo.Data.Attributes.Popularity);

        foreach (var externalLink in artistInfo?.Data?.Attributes?.ExternalLinks ?? [])
        {
            await _tidalRepository.UpsertArtistExternalLinkAsync(artistId, externalLink.Href, externalLink.Meta.Type);
        }

        foreach (var imageLink in artistInfo?.Data?.Attributes?.ImageLinks ?? [])
        {
            await _tidalRepository.UpsertArtistImageLinkAsync(artistId,
                imageLink.Href,
                imageLink.Meta.Width,
                imageLink.Meta.Height);
        }

        return artistInfo;
    }
}