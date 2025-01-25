using MiniMediaScanner.Repositories;
using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class UpdateMBCommandHandler
{
    private readonly MusicBrainzService _musicBrainzService;
    private readonly MusicBrainzArtistRepository _musicBrainzArtistRepository;

    public UpdateMBCommandHandler(string connectionString)
    {
        _musicBrainzService = new MusicBrainzService(connectionString);
        _musicBrainzArtistRepository = new MusicBrainzArtistRepository(connectionString);
    }
    
    public void UpdateMusicBrainzArtistsByName(string artist)
    {
        Console.WriteLine($"Updating artist, {artist}");
        var artistIds = _musicBrainzArtistRepository.GetMusicBrainzArtistRemoteIdsByName(artist);
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