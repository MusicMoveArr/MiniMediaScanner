namespace MiniMediaScanner.Models.MusicBrainz;

public class MusicBrainzSplitArtistModel
{
    public Guid MusicBrainzRemoteId { get; set; }
    public string Name { get; set; }
    public string Country { get; set; }
    public string Type { get; set; }
    public string Date { get; set; }
}