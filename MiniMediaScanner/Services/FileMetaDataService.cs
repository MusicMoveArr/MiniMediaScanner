using ATL;
using FFMpegCore;

namespace MiniMediaScanner.Services;

public class FileMetaDataService
{
    private const string VariousArtistsName = "Various Artists";
    private const string AcoustidFingerprintTag = "acoustid fingerprint";
    private const string AcoustidIdTag = "acoustid id";

    private string[] ignoreTags = new string[]
    {
        "GEOB.",
        "PRIV.TRAKTOR4"
    };
    
    public MetadataInfo GetMetadataInfo(FileInfo fileInfo)
    {
        Track trackInfo = new Track(fileInfo.FullName);
        var mediaTags = trackInfo.AdditionalFields
            .Where(tag => !ignoreTags.Any(ignoreTag => tag.Key.ToLower().StartsWith(ignoreTag.ToLower())))
            .GroupBy(pair => pair.Key.ToLower())
            .Select(pair => pair.First())
            .ToDictionary(StringComparer.OrdinalIgnoreCase);
        
        mediaTags["album"] = trackInfo.Album;
        mediaTags["album_artist"] = trackInfo.AlbumArtist;
        mediaTags["albumartistsortorder"] = trackInfo.SortAlbumArtist;
        
        mediaTags["artist"] = trackInfo.Artist;
        mediaTags["artistsort"] = trackInfo.SortArtist;
        mediaTags["sort_artist"] = trackInfo.SortArtist;
        
        mediaTags["disc"] = trackInfo.DiscNumber?.ToString();
        
        if (trackInfo.OriginalReleaseYear > 0)
        {
            mediaTags["originalyear"] = trackInfo.OriginalReleaseYear.ToString();
        }
        
        if (trackInfo.TrackNumber > 0)
        {
            mediaTags["track"] = trackInfo.TrackNumber?.ToString();
        }
        
        if (trackInfo.TrackTotal > 0)
        {
            mediaTags["totaltracks"] = trackInfo.TrackTotal?.ToString();
        }
        
        if (!string.IsNullOrWhiteSpace(trackInfo.Title))
        {
            mediaTags["title"] = trackInfo.Title;
        }
        if (!string.IsNullOrWhiteSpace(trackInfo.Comment))
        {
            mediaTags["comment"] = trackInfo.Comment;
        }
        if (!string.IsNullOrWhiteSpace(trackInfo.Conductor))
        {
            mediaTags["conductor"] = trackInfo.Conductor;
        }
        if (!string.IsNullOrWhiteSpace(trackInfo.Copyright))
        {
            mediaTags["copyright"] = trackInfo.Copyright;
        }
        if (!string.IsNullOrWhiteSpace(trackInfo.Publisher))
        {
            mediaTags["publisher"] = trackInfo.Publisher;
        }
        if (!string.IsNullOrWhiteSpace(trackInfo.ISRC))
        {
            mediaTags["ISRC"] = trackInfo.ISRC;
        }
        if (trackInfo.Duration > 0)
        {
            mediaTags["duration"] = trackInfo.Duration.ToString();
        }
        if (!string.IsNullOrWhiteSpace(trackInfo.Group))
        {
            mediaTags["group"] = trackInfo.Group;
        }
        
        mediaTags["lyrics"] = trackInfo.Lyrics?.UnsynchronizedLyrics;
        
        if (trackInfo.BPM > 0)
        {
            mediaTags["TBPM"] = trackInfo.BPM.ToString();
            mediaTags["BPM"] = trackInfo.BPM.ToString();
        }
        
        //add all non-AdditionalFields
        trackInfo
            .GetType()
            .GetProperties()
            .ToList()
            .ForEach(prop =>
            {
                object? value = prop.GetValue(trackInfo);
                
                if (value is not null &&
                    (value is string || (value is int val && val > 0)) &&
                    !mediaTags.ContainsKey(prop.Name))
                {
                    mediaTags[prop.Name] = value.ToString();
                }
            });
        
        mediaTags = mediaTags
            .Where(pair => !string.IsNullOrEmpty(pair.Value))
            .ToDictionary(StringComparer.OrdinalIgnoreCase);

        string jsonTags = Newtonsoft.Json.JsonConvert.SerializeObject(mediaTags);
        
        var album = GetValue(mediaTags, "album");
        var artist = GetValue(mediaTags, "album_artist");
        var tempArtistName = GetValue(mediaTags, "artist");
        string sortArtist = GetValue(mediaTags, "artistsort");

        if (string.IsNullOrWhiteSpace(sortArtist))
        {
            sortArtist = GetValue(mediaTags, "sort_artist");
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
        
        string trackTag = GetValue(mediaTags, "track");
        string discTag = GetValue(mediaTags, "disc");
        int track = 0;
        int trackCount = 0;
        int disc = 0;
        int discCount = 0;
        
        int.TryParse(GetValue(mediaTags, "originalyear"), out int originalyear);
        int.TryParse(GetValue(mediaTags, "tbpm"), out int beatsPerMinute);
        
        double.TryParse(GetValue(mediaTags, "replaygain_album_peak"), out double replayGainAlbumPeak);
        double.TryParse(GetValue(mediaTags, "replaygain_track_peak"), out double replayGainTrackPeak);
        double.TryParse(GetValue(mediaTags, "replaygain_album_gain"), out double replayGainAlbumGain);
        double.TryParse(GetValue(mediaTags, "replaygain_track_gain"), out double replayGainTrackGain);
        DateTime.TryParse(GetValue(mediaTags, "date tagged"), out DateTime dateTagged);
        
        if (trackTag?.Contains('/') == true)
        {
            track = int.Parse(trackTag.Split('/')[0]);
            trackCount = int.Parse(trackTag.Split('/')[1]);
        }
        else
        {
            int.TryParse(GetValue(mediaTags, "tracktotal"), out int trackTotal);
            int.TryParse(GetValue(mediaTags, "track"), out int trackValue);
            
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
            int.TryParse(GetValue(mediaTags, "disc"), out int discValue);
            
            disc = discValue;
            if (disc > 0)
            {
                discCount = disc;
            }
        }

        var durationSpan = TimeSpan.FromSeconds(trackInfo.Duration);
        
        return new MetadataInfo
        {
            Path = fileInfo.FullName,
            Album = album,
            Artist = artist,
            Title = GetValue(mediaTags, "title"),
            MusicBrainzArtistId = GetValue(mediaTags, "musicbrainz artist id"),
            MusicBrainzDiscId = GetValue(mediaTags, "musicbrainz disc id"),
            MusicBrainzReleaseCountry = GetValue(mediaTags, "musicbrainz album release country"),
            MusicBrainzReleaseId = GetValue(mediaTags, "musicbrainz album id"),
            MusicBrainzTrackId = GetValue(mediaTags, "musicbrainz release track id"),
            MusicBrainzReleaseStatus = GetValue(mediaTags, "musicbrainz album status"),
            MusicBrainzReleaseType = GetValue(mediaTags, "musicbrainz album type"),
            MusicBrainzReleaseArtistId = GetValue(mediaTags, "musicbrainz album artist id"),
            MusicBrainzReleaseGroupId = GetValue(mediaTags, "musicbrainz release group id"),
            
            Tag_Subtitle = GetValue(mediaTags, "subtitle"),
            Tag_AlbumSort = GetValue(mediaTags, "tso2"),
            Tag_Comment = GetValue(mediaTags, "comment"),
            Tag_Year = originalyear,
            Tag_Track = track,
            Tag_TrackCount = trackCount,
            Tag_Disc = disc,
            Tag_DiscCount = discCount,
            Tag_Lyrics = GetValue(mediaTags, "lyrics"),
            Tag_Grouping = GetValue(mediaTags, "grouping"),
            Tag_BeatsPerMinute = beatsPerMinute,
            Tag_Conductor = GetValue(mediaTags, "conductor"),
            Tag_Copyright = GetValue(mediaTags, "copyright"),
            Tag_DateTagged = dateTagged,
            Tag_AmazonId = GetValue(mediaTags, "amazon id"),
            Tag_ReplayGainTrackGain = replayGainTrackGain,
            Tag_ReplayGainTrackPeak = replayGainTrackPeak,
            Tag_ReplayGainAlbumGain = replayGainAlbumGain,
            Tag_ReplayGainAlbumPeak = replayGainAlbumPeak,
            Tag_InitialKey = GetValue(mediaTags, "initial key"),
            Tag_RemixedBy = GetValue(mediaTags, "remixed by"),
            Tag_Publisher = GetValue(mediaTags, "publisher"),
            Tag_ISRC = GetValue(mediaTags, "isrc"),
            Tag_Length = durationSpan.TotalHours >= 1 ? durationSpan.ToString(@"hh\:mm\:ss") : durationSpan.ToString(@"mm\:ss"),
            Tag_AcoustIdFingerPrint = GetValue(mediaTags, AcoustidFingerprintTag),
            Tag_AcoustId = GetValue(mediaTags, AcoustidIdTag),
            File_LastWriteTime = fileInfo.LastWriteTime,
            File_CreationTime = fileInfo.CreationTime,
            Tag_AllJsonTags = jsonTags
        };
    }

    private string GetValue(Dictionary<string, string> dictionary, string tagName)
    {
        string key = dictionary.Keys.FirstOrDefault(key => string.Equals(key, tagName, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }
        return dictionary[key];
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