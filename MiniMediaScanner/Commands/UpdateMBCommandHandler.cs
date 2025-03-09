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
    
    public void UpdateMusicBrainzArtistsByName(string artistName)
    {
        Console.WriteLine($"Updating artist, {artistName}");
        var artistIds = _musicBrainzArtistRepository.GetMusicBrainzArtistRemoteIdsByName(artistName);

        if (artistIds?.Count == 0)
        {
            artistIds = _musicBrainzAPIService
                .SearchArtist(artistName)
                ?.Artists?
                .Where(artist => artist.Name.ToLower().Contains(artistName))
                .Select(artist => artist.Id)
                .ToList();
        }
        
        artistIds.ForEach(id => UpdateMusicBrainzArtistId(id));
    }
    
    public void UpdateAllMusicBrainzArtists()
    {
        var artistIds = _musicBrainzArtistRepository.GetAllMusicBrainzArtistRemoteIds();
        artistIds.ForEach(id => UpdateMusicBrainzArtistId(id));
    }
    
    public void UpdateMusicBrainzArtistId(string artistId)
    {
        try
        {
            Console.WriteLine($"Updating Music Brainz Artist ID '{artistId}'");
            _musicBrainzService.UpdateMusicBrainzArtist(artistId, true);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}