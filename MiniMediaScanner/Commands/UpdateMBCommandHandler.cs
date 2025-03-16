using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class UpdateMBCommandHandler
{
    private readonly MusicBrainzService _musicBrainzService;
    private readonly MusicBrainzArtistRepository _musicBrainzArtistRepository;
    private readonly MusicBrainzAPIService _musicBrainzAPIService;

    public UpdateMBCommandHandler(string connectionString)
    {
        _musicBrainzService = new MusicBrainzService(connectionString);
        _musicBrainzArtistRepository = new MusicBrainzArtistRepository(connectionString);
        _musicBrainzAPIService = new MusicBrainzAPIService();
    }
    
    public async Task UpdateMusicBrainzArtistsByNameAsync(string artistName)
    {
        Console.WriteLine($"Updating artist, {artistName}");
        var artistIds = await _musicBrainzArtistRepository.GetMusicBrainzArtistRemoteIdsByNameAsync(artistName);

        if (artistIds?.Count == 0)
        {
            artistIds = (await _musicBrainzAPIService
                .SearchArtistAsync(artistName))
                ?.Artists?
                .Where(artist => artist.Name.ToLower().Contains(artistName))
                .Select(artist => artist.Id)
                .ToList();
        }

        foreach (var artistId in artistIds)
        {
            await UpdateMusicBrainzArtistIdAsync(artistId);
        }
    }
    
    public async Task UpdateAllMusicBrainzArtistsAsync()
    {
        var artistIds = await _musicBrainzArtistRepository.GetAllMusicBrainzArtistRemoteIdsAsync();
        foreach (var id in artistIds)
        {
            await UpdateMusicBrainzArtistIdAsync(id);
        }
    }
    
    public async Task UpdateMusicBrainzArtistIdAsync(string artistId)
    {
        try
        {
            Console.WriteLine($"Updating Music Brainz Artist ID '{artistId}'");
            await _musicBrainzService.UpdateMusicBrainzArtistAsync(artistId, true);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}