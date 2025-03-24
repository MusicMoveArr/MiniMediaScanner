namespace MiniMediaScanner.Models.MusicBrainz.MusicBrainzRecordings;

public class MusicBrainzArtistCreditEntityModel
{
    public string? Disambiguation { get; set; }
    public string? TypeId { get; set; }
    public string? Name { get; set; }
    public string? Id { get; set; }
    public string? SortName { get; set; }
    public string? Type { get; set; }
    public string? Country { get; set; } //not officially in musicbrainz model (api response)
}