using System.Diagnostics;
using ATL;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models.MusicBrainz;
using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;
using Newtonsoft.Json.Linq;
using SmartFormat;

namespace MiniMediaScanner.Commands;

public class SplitArtistCommandHandler
{
    private readonly MetadataRepository _metadataRepository;
    private readonly MediaTagWriteService _mediaTagWriteService;
    private readonly ImportCommandHandler _importCommandHandler;
    private readonly StringNormalizerService _normalizerService;
    private readonly MusicBrainzArtistRepository _musicBrainzArtistRepository;

    public SplitArtistCommandHandler(string connectionString)
    {
        _metadataRepository = new MetadataRepository(connectionString);
        _mediaTagWriteService = new MediaTagWriteService();
        _importCommandHandler = new ImportCommandHandler(connectionString);
        _normalizerService = new StringNormalizerService();
        _musicBrainzArtistRepository = new MusicBrainzArtistRepository(connectionString);
    }
    
    public async Task SplitArtistAsync(string artist, string artistFormat, bool autoConfirm)
    {
        var metadata = (await _metadataRepository.GetMetadataByArtistAsync(artist))
            .ToList();

        var musicBrainzArtists = await _musicBrainzArtistRepository.GetSplitBrainzArtistAsync(artist);
        
        Console.WriteLine($"Checking artist '{artist}' ");

        if (metadata
                .GroupBy(x => x.MusicBrainzArtistId)
                .Count() == 1)
        {
            Console.WriteLine("Only found 1 MusicBrainz Artist Id from the metadata, not sure how to split...");
            return;
        }
        if (musicBrainzArtists.Count() == 1)
        {
            Console.WriteLine("Only found 1 MusicBrainz Artist, not sure how to split...");
            return;
        }
        
        var groupedAlbums = metadata.GroupBy(m => m.AlbumId);

        foreach (var album in groupedAlbums)
        {
            string albumName = album.First().AlbumName;
            int artistCount = album.GroupBy(x => x.MusicBrainzArtistId).Count();
            if (artistCount > 1)
            {
                Console.WriteLine($"Album {albumName} has {artistCount} artists...");
                continue;
            }
            
            Guid.TryParse(album.First().MusicBrainzArtistId, out var musicBrainzArtistId);
            
            var musicBrainzArtist = musicBrainzArtists
                .Where(m => GuidHelper.GuidHasValue(m.MusicBrainzRemoteId))
                .FirstOrDefault(artist => artist.MusicBrainzRemoteId == musicBrainzArtistId);
            
            if (musicBrainzArtist == null)
            {
                Console.WriteLine($"No data about MusicBrainzArtistId '{musicBrainzArtistId}', couldn't process {album.Count()} Tracks of album '{albumName}'");
                continue;
            }

            string newArtistName = Smart.Format(artistFormat, musicBrainzArtist).Trim();
            
            foreach (var metadataAlbum in album)
            {
                Console.WriteLine($"Artist: {newArtistName}, Album: {albumName}, Path: {metadataAlbum.Path}");
            }
            
            Console.WriteLine("Confirm changes? (Y/y or N/n)");
            bool confirm = autoConfirm || Console.ReadLine()?.ToLower() == "y";
            if (!confirm)
            {
                continue;
            }

            foreach (var metadataAlbum in album)
            {
                Track track = new Track(metadataAlbum.Path);
                bool trackInfoUpdated = false;
                if (string.Equals(track.AlbumArtist, artist, StringComparison.OrdinalIgnoreCase))
                {
                    _mediaTagWriteService.UpdateTag(track, "AlbumArtist", newArtistName, ref trackInfoUpdated, true);
                }
                if (string.Equals(track.Artist, artist, StringComparison.OrdinalIgnoreCase))
                {
                    _mediaTagWriteService.UpdateTag(track, "Artist", newArtistName, ref trackInfoUpdated, true);
                }
                if (string.Equals(track.SortArtist, artist, StringComparison.OrdinalIgnoreCase))
                {
                    _mediaTagWriteService.UpdateTag(track, "SortArtist", newArtistName, ref trackInfoUpdated, true);
                }
                if (string.Equals(track.SortAlbumArtist, artist, StringComparison.OrdinalIgnoreCase))
                {
                    _mediaTagWriteService.UpdateTag(track, "SortAlbumArtist", newArtistName, ref trackInfoUpdated, true);
                }
                
                if (trackInfoUpdated && await _mediaTagWriteService.SafeSaveAsync(track))
                {
                    await _importCommandHandler.ProcessFileAsync(metadataAlbum.Path);
                }
            }
        }
    }
}