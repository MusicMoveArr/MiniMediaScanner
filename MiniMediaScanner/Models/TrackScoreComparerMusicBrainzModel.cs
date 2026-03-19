using MiniMediaScanner.Interfaces;
using MiniMediaScanner.Models.Deezer;
using MiniMediaScanner.Models.MusicBrainz;

namespace MiniMediaScanner.Models;

public class TrackScoreComparerMusicBrainzModel : ITrackScoreComparerModel
{
    public MusicBrainzTrackDbModel TrackModel { get; private set; }

    public TrackScoreComparerMusicBrainzModel(MusicBrainzTrackDbModel trackModel)
    {
        this.TrackModel = trackModel;
    }

    public string Artist => TrackModel.ArtistName;
    public string Album => TrackModel.AlbumName;
    public string AlbumId => TrackModel.AlbumId.ToString();
    public string Title => TrackModel.TrackName;
    public TimeSpan Duration => TrackModel.Duration;
    public string Isrc => string.Empty;
    public string Upc => TrackModel.AlbumUPC;
    public string Date => TrackModel.AlbumReleaseDate;
    public int TrackNumber => TrackModel.TrackPosition;
    public int TrackTotalCount => 0;
}