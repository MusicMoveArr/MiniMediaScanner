using MiniMediaScanner.Services;

namespace MiniMediaScanner.Commands;

public class UpdateMBCommandHandler
{
    private readonly DatabaseService _databaseService;
    private readonly MusicBrainzService _musicBrainzService;

    public UpdateMBCommandHandler(string connectionString)
    {
        _databaseService = new DatabaseService(connectionString);
        _musicBrainzService = new MusicBrainzService(connectionString);
    }
    
    public void UpdateMusicBrainzArtistsByName(List<string> names)
    {
        var artistIds = _databaseService.GetMusicBrainzArtistRemoteIdsByName(names);
        artistIds.ForEach(id => UpdateMusicBrainzArtistId(id));
    }
    
    public void UpdateAllMusicBrainzArtists()
    {
        var artistIds = _databaseService.GetAllMusicBrainzArtistRemoteIds();
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