namespace MiniMediaScanner.Models.MusicBrainz.MusicBrainzRecordings;

public class MusicBrainzRecordingModel
{
    public List<MusicBrainzArtistRelationEntityModel>? Relations { get; set; }
    public bool Video { get; set; }
    public List<MusicBrainzArtistCreditModel>? ArtistCredit { get; set; }
}