using FuzzySharp;
using MiniMediaScanner.Callbacks;
using MiniMediaScanner.Callbacks.Status;
using MiniMediaScanner.Enums;
using MiniMediaScanner.JsonConverters;
using MiniMediaScanner.Models;
using MiniMediaScanner.Models.Tidal;
using MiniMediaScanner.Repositories;
using SoundCloudExplode;
using SoundCloudExplode.Common;
using SoundCloudExplode.Playlists;
using SoundCloudExplode.Search;
using SoundCloudExplode.Tracks;

namespace MiniMediaScanner.Services;

public class SoundCloudService
{
    private readonly SoundCloudClient _soundcloud;
    public readonly int PreventUpdateWithinDays; 
    private readonly string _connectionString;

    public SoundCloudService(string connectionString, 
        string clientId,
        int preventUpdateWithinDays)
    {
        _soundcloud = new SoundCloudClient(clientId);
        _connectionString =  connectionString;
        this.PreventUpdateWithinDays = preventUpdateWithinDays;
    }
    
    public async Task UpdateArtistByNameAsync(string artistName,
        Action<UpdateSoundCloudCallback>? callback = null)
    {
        var searchResult = _soundcloud.Search.GetUsersAsync(artistName).GetAwaiter().GetResult();

        foreach (var user in searchResult
                     .Where(user => user.Id.HasValue)
                     ?.Where(artist => !string.IsNullOrWhiteSpace(artist?.Username))
                     ?.OrderByDescending(artist => Fuzz.Ratio(artistName, artist.Title))
                     ?.Where(artist => Fuzz.Ratio(artistName, artist.Title) > 80) ?? [])
        {
            try
            {
                await UpdateArtistByIdAsync(user, callback);
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

    public async Task UpdateArtistByIdAsync(long userId,
        Action<UpdateSoundCloudCallback>? callback = null)
    {
        
    }

    public async Task UpdateArtistByIdAsync(UserSearchResult userSearchResult,
        Action<UpdateSoundCloudCallback>? callback = null)
    {
        UpdateSoundCloudRepository updateSoundCloudRepository = new UpdateSoundCloudRepository(_connectionString);
        await updateSoundCloudRepository.SetConnectionAsync();
        
        DateTime? lastSyncTime = await updateSoundCloudRepository.GetArtistLastSyncTimeAsync(userSearchResult.Id.Value);
        if (lastSyncTime?.Year > 2000 && DateTime.Now.Subtract(lastSyncTime.Value).TotalDays < this.PreventUpdateWithinDays)
        {
            await updateSoundCloudRepository.CommitAsync();
            callback?.Invoke(new UpdateSoundCloudCallback(userSearchResult.Id.Value, UpdateSoundCloudStatus.SkippedSyncedWithin));
            return;
        }

        await updateSoundCloudRepository.UpsertUserAsync(userSearchResult);
        
        var allAlbums = _soundcloud.Users.GetAlbumsAsync(userSearchResult.Url).GetAwaiter().GetResult();
        var albumIds = allAlbums
            .Where(album => album.Id.HasValue)
            .Select(album => album.Id.Value)
            .ToList();
        
        var allPlaylists = _soundcloud.Users
            .GetPlaylistsAsync(userSearchResult.Url)
            .GetAwaiter()
            .GetResult()
            .Where(playlist => playlist.Id.HasValue)
            .Where(playlist => !albumIds.Contains(playlist.Id.Value))
            .ToList();

        int progress = 0;
        foreach (var album in allAlbums.Where(p => p.Id.HasValue))
        {
            callback?.Invoke(new UpdateSoundCloudCallback(
                userSearchResult.Id.Value, 
                album.User?.Username ?? string.Empty,
                album.Title,
                allAlbums.Count,
                UpdateSoundCloudStatus.Updating,
                progress++));
            
            await updateSoundCloudRepository.UpsertPlaylistAsync(album);

            long trackOrder = 1;
            foreach (var track in album.Tracks.Where(t => t.UserId.HasValue))
            {
                await ProcessTrackAsync(track, album, trackOrder, updateSoundCloudRepository);
                trackOrder++;
            }
        }
        
        progress = 0;
        foreach (var playlist in allPlaylists)
        {
            callback?.Invoke(new UpdateSoundCloudCallback(
                userSearchResult.Id.Value, 
                playlist.User?.Username ?? string.Empty,
                playlist.Title,
                allPlaylists.Count,
                UpdateSoundCloudStatus.Updating,
                progress++));
            await updateSoundCloudRepository.UpsertPlaylistAsync(playlist);
            long trackOrder = 1;
            foreach (var track in playlist.Tracks.Where(t => t.UserId.HasValue))
            {
                await ProcessTrackAsync(track, playlist, trackOrder, updateSoundCloudRepository);
                trackOrder++;
            }
        }

        await updateSoundCloudRepository.SetArtistLastSyncTimeAsync(userSearchResult.Id.Value);
        await updateSoundCloudRepository.CommitAsync();
    }

    private async Task ProcessTrackAsync(Track track, 
        Playlist playlist,
        long trackOrder,
        UpdateSoundCloudRepository updateSoundCloudRepository)
    {
        await updateSoundCloudRepository.UpsertTrackAsync(track);
        await updateSoundCloudRepository.UpsertPlaylistTrackAsync(playlist.UserId, playlist.Id.Value, track.Id, trackOrder);
    }
}