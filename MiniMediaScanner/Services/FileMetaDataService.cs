using ATL;
using FFMpegCore;

namespace MiniMediaScanner.Services;

public class FileMetaDataService
{
    private const string VariousArtistsName = "Various Artists";
    private const string AcoustidFingerprintTag = "acoustid fingerprint";
    private const string AcoustidIdTag = "acoustid id";
    
    public MetadataInfo GetMetadataInfo(FileInfo fileInfo)
    {
        Track trackInfo = new Track(fileInfo.FullName);
        var mediaTags = trackInfo.AdditionalFields
            .GroupBy(pair => pair.Key.ToLower())
            .Select(pair => pair.First())
            .ToDictionary(StringComparer.OrdinalIgnoreCase);
        
        mediaTags["album"] = trackInfo.Album;
        mediaTags["albumartist"] = trackInfo.AlbumArtist;
        mediaTags["albumartistsortorder"] = trackInfo.SortAlbumArtist;
        
        mediaTags["artist"] = trackInfo.Artist;
        mediaTags["artistsort"] = trackInfo.SortArtist;
        mediaTags["sort_artist"] = trackInfo.SortArtist;
        
        mediaTags["disc"] = trackInfo.DiscNumber?.ToString();
        
        if (trackInfo.OriginalReleaseYear.HasValue)
        {
            mediaTags["originalyear"] = trackInfo.OriginalReleaseYear.ToString();
        }
        
        mediaTags["track"] = trackInfo.TrackNumber?.ToString();
        mediaTags["totaltracks"] = trackInfo.TrackTotal?.ToString();
        mediaTags["title"] = trackInfo.Title;
        
        mediaTags["comment"] = trackInfo.Comment;
        mediaTags["lyrics"] = trackInfo.Lyrics?.ToString();
        mediaTags["conductor"] = trackInfo.Conductor;
        mediaTags["copyright"] = trackInfo.Copyright;
        mediaTags["publisher"] = trackInfo.Publisher;
        mediaTags["ISRC"] = trackInfo.ISRC;
        mediaTags["duration"] = trackInfo.Duration.ToString();
        mediaTags["group"] = trackInfo.Group;
        
        if (trackInfo.BPM.HasValue)
        {
            mediaTags["TBPM"] = trackInfo.BPM.ToString();
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
                    (value is string || value is int) &&
                    !mediaTags.ContainsKey(prop.Name))
                {
                    mediaTags[prop.Name] = value.ToString();
                }
            });
        
        mediaTags = mediaTags
            .Where(pair => !string.IsNullOrEmpty(pair.Value))
            .ToDictionary(StringComparer.OrdinalIgnoreCase);
        
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

        var durationSpan = TimeSpan.FromSeconds(trackInfo.Duration);
        
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
            TagLength = durationSpan.TotalHours >= 1 ? durationSpan.ToString(@"hh\:mm\:ss") : durationSpan.ToString(@"mm\:ss"),
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