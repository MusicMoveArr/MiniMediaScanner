using System.Diagnostics;
using FuzzySharp;
using MiniMediaScanner.Callbacks;
using MiniMediaScanner.Callbacks.Status;
using MiniMediaScanner.Models.Deezer;
using MiniMediaScanner.Models.Tidal;
using MiniMediaScanner.Repositories;
using SmartFormat.Utilities;

namespace MiniMediaScanner.Services;

public class DeezerService
{
    private const int PreventUpdateWithinDays = 7; 
    private readonly DeezerAPIService _deezerAPIService;
    private readonly DeezerRepository _deezerRepository;

    public DeezerService(string connectionString)
    {
        _deezerRepository = new DeezerRepository(connectionString);
        _deezerAPIService = new DeezerAPIService();
    }
    
    public async Task UpdateArtistByNameAsync(string artistName,
        Action<UpdateDeezerCallback>? callback = null)
    {
        var searchResult = await _deezerAPIService.SearchResultsArtistsAsync(artistName);

        if (searchResult?.Data.Any() == true)
        {
            foreach (var artist in searchResult
                         .Data
                         .Where(artist => !string.IsNullOrWhiteSpace(artist.Name))
                         .Where(artist => Fuzz.Ratio(artistName, artist.Name) > 80))
            {
                try
                {
                    await UpdateArtistByIdAsync(artist.Id, callback);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{e.Message}, {e.StackTrace}");
                }
            }
        }
    }

    public async Task UpdateArtistByIdAsync(long artistId,
        Action<UpdateDeezerCallback>? callback = null)
    {
        DateTime? lastSyncTime = await _deezerRepository.GetArtistLastSyncTimeAsync(artistId);
        if (lastSyncTime?.Year > 2000 && DateTime.Now.Subtract(lastSyncTime.Value).TotalDays < PreventUpdateWithinDays)
        {
            callback?.Invoke(new UpdateDeezerCallback(artistId, UpdateDeezerStatus.SkippedSyncedWithin));
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
        var albums = await _deezerAPIService.GetAlbumsByArtistIdAsync(artistId);

        if (!string.IsNullOrWhiteSpace(albums.Next))
        {
            string? nextUrl = albums.Next;
            while (!string.IsNullOrWhiteSpace(nextUrl))
            {
                callback?.Invoke(new UpdateDeezerCallback(artistId, 
                    artistInfo.Name,
                    string.Empty,
                    albums.Data.Count,
                    UpdateDeezerStatus.Updating,
                    albums.Data.Count,
                    $"Fetching all albums... {albums.Data.Count}"));
                
                var nextAlbums = await _deezerAPIService.GetAlbumsNextAsync(nextUrl);
                albums.Data.AddRange(nextAlbums.Data);
                nextUrl = nextAlbums?.Next;
            }
        }
        
        int albumProgress = 1;
        foreach (var album in albums.Data)
        {
            callback?.Invoke(new UpdateDeezerCallback(artistId,
                artistInfo.Name,
                album.Title,
                albums.Data.Count,
                UpdateDeezerStatus.Updating,
                albumProgress++));

            var fullAlbumInfo = await _deezerAPIService.GetAlbumByIdAsync(album.Id);

            if (fullAlbumInfo.Id == 0)
            {
                //album not found... how?
                continue;
            }
            
            //insert album info
            await _deezerRepository.UpsertAlbumAsync(fullAlbumInfo.Id,
                fullAlbumInfo.Artist.Id,
                fullAlbumInfo.Title,
                fullAlbumInfo.Md5Image,
                fullAlbumInfo.GenreId,
                fullAlbumInfo.Fans,
                fullAlbumInfo.ReleaseDate,
                fullAlbumInfo.RecordType,
                fullAlbumInfo.ExplicitLyrics,
                fullAlbumInfo.Type,
                fullAlbumInfo.ExplicitContentLyrics,
                fullAlbumInfo.ExplicitContentCover,
                fullAlbumInfo.UPC,
                fullAlbumInfo.Label,
                fullAlbumInfo.NbTracks,
                fullAlbumInfo.Duration,
                fullAlbumInfo.Available);

            await _deezerRepository.UpsertAlbumImageLinkAsync(album.Id, album.Cover, "cover");
            await _deezerRepository.UpsertAlbumImageLinkAsync(album.Id, album.CoverBig, "big");
            await _deezerRepository.UpsertAlbumImageLinkAsync(album.Id, album.CoverMedium, "medium");
            await _deezerRepository.UpsertAlbumImageLinkAsync(album.Id, album.CoverSmall, "small");
            await _deezerRepository.UpsertAlbumImageLinkAsync(album.Id, album.CoverXL, "xl");

            foreach (var genre in fullAlbumInfo.Genres.Data)
            {
                await _deezerRepository.UpsertGenreAsync(genre.Id, genre.Name, genre.Picture, genre.Type);
                await _deezerRepository.UpsertAlbumGenreAsync(album.Id, genre.Id);
            }
            
            foreach (var contributor in fullAlbumInfo.Contributors)
            {
                await InsertArtistInfoAsync(contributor.Id);
                    
                await _deezerRepository.UpsertAlbumArtistIdAsync(fullAlbumInfo.Id, 
                    contributor.Id,
                    contributor.Role);
            }
            
            var tracks = await _deezerAPIService.GetTracksByAlbumIdAsync(album.Id);

            //get all tracks
            if (!string.IsNullOrWhiteSpace(tracks.Next))
            {
                string? nextUrl = tracks.Next;
                while (!string.IsNullOrWhiteSpace(nextUrl))
                {
                    callback?.Invoke(new UpdateDeezerCallback(artistId, 
                        artistInfo.Name,
                        album.Title,
                        albums.Data.Count,
                        UpdateDeezerStatus.Updating,
                        albumProgress,
                        $"Fetching all tracks of album '{album.Title}', {tracks.Data.Count}"));
                    
                    var nextTracks = await _deezerAPIService.GetAlbumTracksNextAsync(nextUrl);
                    tracks.Data.AddRange(nextTracks.Data);
                    nextUrl = nextTracks?.Next;
                }
            }
            
            //I wish I can use nb_tracks of the album, but nb_tracks mismatches the amount of tracks I get through the API
            int dbTrackCount = await _deezerRepository.GetAlbumTrackCountAsync(album.Id, artistId);
            if (dbTrackCount == tracks.Data.Count) 
            {
                continue;
            }

            int trackProgress = 1;
            foreach (var track in tracks.Data)
            {
                callback?.Invoke(new UpdateDeezerCallback(artistId, 
                    artistInfo.Name,
                    album.Title,
                    albums.Data.Count,
                    UpdateDeezerStatus.Updating,
                    albumProgress,
                    $"Processing tracks of album '{album.Title}', {trackProgress++} / {tracks.Data.Count}"));
                
                var fullTrackInfo = await _deezerAPIService.GetTrackByIdAsync(track.Id);

                if (fullTrackInfo.Id == 0) //track was not found
                {
                    continue;
                }
                
                await _deezerRepository.UpsertTrackAsync(fullTrackInfo.Id,
                    fullTrackInfo.Album.Id,
                    artistId, //use this artist id on purpose
                    fullTrackInfo.Readable,
                    fullTrackInfo.Title,
                    fullTrackInfo.TitleShort,
                    fullTrackInfo.TitleVersion ?? string.Empty,
                    fullTrackInfo.ISRC,
                    fullTrackInfo.Duration,
                    fullTrackInfo.TrackPosition,
                    fullTrackInfo.DiskNumber,
                    fullTrackInfo.Rank,
                    fullTrackInfo.ReleaseDate,
                    fullTrackInfo.ExplicitLyrics,
                    fullTrackInfo.ExplicitContentLyrics,
                    fullTrackInfo.ExplicitContentCover,
                    fullTrackInfo.Preview,
                    fullTrackInfo.BPM,
                    fullTrackInfo.Gain,
                    fullTrackInfo.Md5Image,
                    fullTrackInfo.TrackToken,
                    fullTrackInfo.Type);
                
                //add the search ArtistId on purpose to the track_artist
                //add if needed the ArtistId from fullTrackInfo
                //and then add the rest of the contributors
                //I do this on purpose to let the original artistid intact with how it's shown in deezer
                //collections can break because we grab the individual tracks
                //individual tracks are not attached perse to an album/collection
                //Label Monstercat has a huge collection of Albums of other Artists, not own releases, they're collections
                
                await _deezerRepository.UpsertTrackArtistIdAsync(fullTrackInfo.Id, 
                    artistId,
                    fullTrackInfo.Album.Id);
                
                await _deezerRepository.UpsertTrackArtistIdAsync(fullTrackInfo.Id, 
                    fullTrackInfo.Artist.Id,
                    fullTrackInfo.Album.Id);

                foreach (var contributor in fullTrackInfo.Contributors)
                {
                    await InsertArtistInfoAsync(contributor.Id);
                    
                    await _deezerRepository.UpsertTrackArtistIdAsync(fullTrackInfo.Id, 
                        contributor.Id,
                        fullTrackInfo.Album.Id);
                }
            }
        }

        await _deezerRepository.SetArtistLastSyncTimeAsync(artistId);
    }

    private async Task<DeezerSearchArtistModel?> InsertArtistInfoAsync(long artistId, bool ignorePeventCheck = false)
    {
        if (!ignorePeventCheck && await _deezerRepository.ArtistExistsByIdAsync(artistId))
        {
           return null;
        }
        
        var artistInfo = await _deezerAPIService.GetArtistInfoByIdAsync(artistId);

        if (artistInfo == null || string.IsNullOrWhiteSpace(artistInfo.Name))
        {
            return null;
        }
        
        await _deezerRepository.UpsertArtistAsync(artistId,
            artistInfo.Name,
            artistInfo.NbAlbum,
            artistInfo.NbFan,
            artistInfo.Radio,
            artistInfo.Type);

        await _deezerRepository.UpsertArtistImageLinkAsync(artistId, artistInfo.Picture, "picture");
        await _deezerRepository.UpsertArtistImageLinkAsync(artistId, artistInfo.PictureBig, "big");
        await _deezerRepository.UpsertArtistImageLinkAsync(artistId, artistInfo.PictureMedium, "medium");
        await _deezerRepository.UpsertArtistImageLinkAsync(artistId, artistInfo.PictureSmall, "small");
        await _deezerRepository.UpsertArtistImageLinkAsync(artistId, artistInfo.PictureXL, "xl");

        return artistInfo;
    }
}