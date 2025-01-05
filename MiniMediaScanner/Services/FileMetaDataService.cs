using System.Text;
using FFMpegCore;

namespace MiniMediaScanner.Services;

public class FileMetaDataService
{
    private const string VariousArtistsName = "Various Artists";
    private const string AcoustidFingerprintTag = "acoustid fingerprint";
    private const string AcoustidIdTag = "acoustid id";
    
    
    public MetadataInfo GetMetadataInfo(FileInfo fileInfo)
    {
        var mediaInfo = FFProbe.Analyse(fileInfo.FullName);
        var mediaTags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var audioStreamTags = mediaInfo.AudioStreams.FirstOrDefault().Tags.ToDictionary(StringComparer.OrdinalIgnoreCase);
        var formatTags = mediaInfo.Format.Tags.ToDictionary(StringComparer.OrdinalIgnoreCase);
        
        foreach (var pair in audioStreamTags)
            mediaTags[pair.Key.ToLower()] = pair.Value;

        foreach (var pair in formatTags)
            mediaTags[pair.Key.ToLower()] = pair.Value;
        
        string jsonTags = Newtonsoft.Json.JsonConvert.SerializeObject(mediaTags);
        
        var album = mediaTags.FirstOrDefault(tag => tag.Key == "album").Value;
        var artist = mediaTags.FirstOrDefault(tag => tag.Key == "album_artist").Value;
        var tempArtistName = mediaTags.FirstOrDefault(tag => tag.Key == "artist").Value;
        string sortArtist = mediaTags.FirstOrDefault(tag => tag.Key == "artistsort").Value;

        if (string.IsNullOrWhiteSpace(sortArtist))
        {
            sortArtist = mediaTags.FirstOrDefault(tag => tag.Key == "sort_artist").Value;
        }
        
        if (string.IsNullOrWhiteSpace(artist) ||
            (artist.Contains(VariousArtistsName) && !string.IsNullOrWhiteSpace(tempArtistName)))
        {
            artist = tempArtistName;
        }
        
        if (string.IsNullOrWhiteSpace(artist) ||
            (artist.Contains(VariousArtistsName) && !string.IsNullOrWhiteSpace(sortArtist)))
        {
            artist = sortArtist;
        }

        artist = GetWithoutFeat(artist);

        if (string.IsNullOrWhiteSpace(artist))
        {
            artist = "[Unknown]";
        }
        if (string.IsNullOrWhiteSpace(album))
        {
            album = "[Unknown]";
        }
        
        string trackTag = mediaTags.FirstOrDefault(tag => tag.Key == "track").Value;
        string discTag = mediaTags.FirstOrDefault(tag => tag.Key == "disc").Value;
        int track = 0;
        int trackCount = 0;
        int disc = 0;
        int discCount = 0;
        
        int.TryParse(mediaTags.FirstOrDefault(tag => tag.Key == "originalyear").Value, out int originalyear);
        int.TryParse(mediaTags.FirstOrDefault(tag => tag.Key == "tbpm").Value, out int beatsPerMinute);
        
        double.TryParse(mediaTags.FirstOrDefault(tag => tag.Key == "replaygain_album_peak").Value, out double replayGainAlbumPeak);
        double.TryParse(mediaTags.FirstOrDefault(tag => tag.Key == "replaygain_track_peak").Value, out double replayGainTrackPeak);
        double.TryParse(mediaTags.FirstOrDefault(tag => tag.Key == "replaygain_album_gain").Value, out double replayGainAlbumGain);
        double.TryParse(mediaTags.FirstOrDefault(tag => tag.Key == "replaygain_track_gain").Value, out double replayGainTrackGain);
        DateTime.TryParse(mediaTags.FirstOrDefault(tag => tag.Key == "date tagged").Value, out DateTime dateTagged);
        
        if (trackTag?.Contains('/') == true)
        {
            track = int.Parse(trackTag.Split('/')[0]);
            trackCount = int.Parse(trackTag.Split('/')[1]);
        }
        else
        {
            int.TryParse(mediaTags.FirstOrDefault(tag => tag.Key == "tracktotal").Value, out int trackTotal);
            int.TryParse(mediaTags.FirstOrDefault(tag => tag.Key == "track").Value, out int trackValue);
            
            track = trackValue;
            trackCount = trackTotal;
        }
        
        if (discTag?.Contains('/') == true)
        {
            int.TryParse(discTag.Split('/')[0], out disc);
            int.TryParse(discTag.Split('/')[1], out discCount);
        }
        else
        {
            int.TryParse(mediaTags.FirstOrDefault(tag => tag.Key == "disc").Value, out int discValue);
            
            disc = discValue;
            if (disc > 0)
            {
                discCount = disc;
            }
        }
        
        return new MetadataInfo
        {
            Path = fileInfo.FullName,
            Album = album,
            Artist = artist,
            Title = mediaTags.FirstOrDefault(tag => tag.Key == "title").Value,
            MusicBrainzArtistId = mediaTags.FirstOrDefault(tag => tag.Key == "musicbrainz artist id").Value,
            MusicBrainzDiscId = mediaTags.FirstOrDefault(tag => tag.Key == "musicbrainz disc id").Value,
            MusicBrainzReleaseCountry = mediaTags.FirstOrDefault(tag => tag.Key == "musicbrainz album release country").Value,
            MusicBrainzReleaseId = mediaTags.FirstOrDefault(tag => tag.Key == "musicbrainz album id").Value,
            MusicBrainzTrackId = mediaTags.FirstOrDefault(tag => tag.Key == "musicbrainz release track id").Value,
            MusicBrainzReleaseStatus = mediaTags.FirstOrDefault(tag => tag.Key == "musicbrainz album status").Value,
            MusicBrainzReleaseType = mediaTags.FirstOrDefault(tag => tag.Key == "musicbrainz album type").Value,
            MusicBrainzReleaseArtistId = mediaTags.FirstOrDefault(tag => tag.Key == "musicbrainz album artist id").Value,
            MusicBrainzReleaseGroupId = mediaTags.FirstOrDefault(tag => tag.Key == "musicbrainz release group id").Value,
            
            TagSubtitle = mediaTags.FirstOrDefault(tag => tag.Key == "subtitle").Value,
            TagAlbumSort = mediaTags.FirstOrDefault(tag => tag.Key == "tso2").Value,
            TagComment = mediaTags.FirstOrDefault(tag => tag.Key == "comment").Value,
            TagYear = originalyear,
            TagTrack = track,
            TagTrackCount = trackCount,
            TagDisc = disc,
            TagDiscCount = discCount,
            TagLyrics = mediaTags.FirstOrDefault(tag => tag.Key == "lyrics").Value,
            TagGrouping = mediaTags.FirstOrDefault(tag => tag.Key == "grouping").Value,
            TagBeatsPerMinute = beatsPerMinute,
            TagConductor = mediaTags.FirstOrDefault(tag => tag.Key == "conductor").Value,
            TagCopyright = mediaTags.FirstOrDefault(tag => tag.Key == "copyright").Value,
            TagDateTagged = dateTagged,
            TagAmazonId = mediaTags.FirstOrDefault(tag => tag.Key == "amazon id").Value,
            TagReplayGainTrackGain = replayGainTrackGain,
            TagReplayGainTrackPeak = replayGainTrackPeak,
            TagReplayGainAlbumGain = replayGainAlbumGain,
            TagReplayGainAlbumPeak = replayGainAlbumPeak,
            TagInitialKey = mediaTags.FirstOrDefault(tag => tag.Key == "initial key").Value,
            TagRemixedBy = mediaTags.FirstOrDefault(tag => tag.Key == "remixed by").Value,
            TagPublisher = mediaTags.FirstOrDefault(tag => tag.Key == "publisher").Value,
            TagISRC = mediaTags.FirstOrDefault(tag => tag.Key == "isrc").Value,
            TagLength =  mediaInfo.Duration.TotalHours >= 1 ? mediaInfo.Duration.ToString(@"hh\:mm\:ss") : mediaInfo.Duration.ToString(@"mm\:ss"),
            TagAcoustIdFingerPrint = mediaTags.FirstOrDefault(tag => tag.Key == AcoustidFingerprintTag).Value,
            TagAcoustId = mediaTags.FirstOrDefault(tag => tag.Key == AcoustidIdTag).Value,
            FileLastWriteTime = fileInfo.LastWriteTime,
            FileCreationTime = fileInfo.CreationTime,
            AllJsonTags = jsonTags
        };
    }

    private string GetWithoutFeat(string artist)
    {
        if (string.IsNullOrWhiteSpace(artist))
        {
            return string.Empty;
        }
        
        var splitCharacters = new string[]
        {
            ",",
            "&",
            "+",
            "/",
            " feat",
            ";"
        };

        string? newArtistName = splitCharacters
            .Where(splitChar => artist.Contains(splitChar))
            .Select(splitChar => artist.Substring(0, artist.IndexOf(splitChar)).Trim())
            .OrderBy(split => split.Length)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(newArtistName))
        {
            return newArtistName;
        }

        return artist;
    }
}