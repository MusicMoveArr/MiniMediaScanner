using ATL;

namespace MiniMediaScanner.Services;

public class MediaTagWriteService
{
    private readonly StringNormalizerService _normalizerService;

    public MediaTagWriteService()
    {
        _normalizerService = new StringNormalizerService();
    }
    
    public async Task<bool> SaveAsync(FileInfo targetFile, string artistName, string albumName, string title)
    {
        string orgValue = string.Empty;
        bool isUpdated = false;
        Track track = new Track(targetFile.FullName);
        var fileMetaDataService = new FileMetaDataService();
        var metadataInfo = fileMetaDataService.GetMetadataInfo(track);
        
        UpdateTrackTag(track, "artist", artistName, ref isUpdated, ref orgValue, metadataInfo);
        UpdateTrackTag(track, "album", albumName, ref isUpdated, ref orgValue, metadataInfo);
        UpdateTrackTag(track, "title", title, ref isUpdated, ref orgValue, metadataInfo);

        return await SafeSaveAsync(track);
    }
    
    public async Task<bool> SaveTagAsync(FileInfo targetFile, string tag, string value)
    {
        string orgValue = string.Empty;
        bool isUpdated = false;
        Track track = new Track(targetFile.FullName);
        var fileMetaDataService = new FileMetaDataService();
        var metadataInfo = fileMetaDataService.GetMetadataInfo(track);
        UpdateTrackTag(track, tag, value, ref isUpdated, ref orgValue, metadataInfo);

        return await SafeSaveAsync(track);
    }

    public bool UpdateTrackTag(
        Track track, 
        string tag, 
        string value, 
        ref bool updated, 
        ref string orgValue,
        MetadataInfo metadataInfo)
    {
        value = value.Trim();
        var oldValues = metadataInfo.MediaTags;
        switch (tag.ToLower())
        {
            case "title":
                orgValue = track.Title;
                updated = !string.Equals(track.Title, value);
                track.Title = value;
                return true;
            case "album":
                orgValue = track.Album;
                updated = !string.Equals(track.Album, value);
                track.Album = value;
                return true;
            case "albumartist":
            case "album_artist":
                orgValue = track.AlbumArtist;
                updated = !string.Equals(track.AlbumArtist, value);
                track.AlbumArtist = value;
                return true;
            case "albumartistsortorder":
            case "sort_album_artist":
            case "sortalbumartist":
                orgValue = track.SortAlbumArtist;
                updated = !string.Equals(track.SortAlbumArtist, value);
                track.SortAlbumArtist = value;
                return true;
            case "albumartistsort":
                orgValue = GetDictionaryValue(oldValues, "ALBUMARTISTSORT");
                track.AdditionalFields[GetFieldName(track,"ALBUMARTISTSORT")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "artistsort":
            case "artist-sort":
            case "sort_artist":
            case "artistsortorder":
            case "sortartist":
                orgValue = track.SortArtist;
                updated = !string.Equals(track.SortArtist, value);
                track.SortArtist = value;
                return true;
            case "artists":
                orgValue = GetDictionaryValue(oldValues, "ARTISTS");
                track.AdditionalFields[GetFieldName(track,"ARTISTS")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "artists_sort":
                orgValue = GetDictionaryValue(oldValues, "ARTISTS_SORT");
                track.AdditionalFields[GetFieldName(track,"ARTISTS_SORT")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "artist":
                orgValue = track.Artist;
                updated = !string.Equals(track.Artist, value);
                track.Artist = value;
                return true;
            case "date":
                orgValue = GetDictionaryValue(oldValues, "date");
                if (DateTime.TryParse(value, out var result))
                {
                    DateTime? oldDate = track.Date;
                    track.AdditionalFields[GetFieldName(track,"date")] = value;
                    track.Date = result;
                    updated = track.Date != oldDate;
                    return true;
                }
                else if (int.TryParse(value, out var result2))
                {
                    int? oldYear = track.Year;
                    track.Year = result2;
                    track.AdditionalFields[GetFieldName(track,"date")] = value;
                    updated = track.Year != oldYear || !string.Equals(orgValue, value);;
                    return true;
                }
                return false;
            case "catalognumber":
                if (!string.Equals(value, "[None]", StringComparison.OrdinalIgnoreCase))
                {
                    orgValue = track.CatalogNumber;
                    updated = !string.Equals(track.CatalogNumber, value);
                    track.CatalogNumber = value;
                }
                return true;
            case "asin":
                orgValue = GetDictionaryValue(oldValues, "asin");
                track.AdditionalFields[GetFieldName(track,"asin")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "year":
                orgValue = track.Year?.ToString() ?? string.Empty;
                if (!int.TryParse(value, out int year))
                {
                    return false;
                }

                updated = track.Year != year;
                track.Year = year;
                return true;
            case "originalyear":
                orgValue = GetDictionaryValue(oldValues, "originalyear");
                if (!int.TryParse(value, out int originalyear))
                {
                    return false;
                }
                track.AdditionalFields[GetFieldName(track,"originalyear")] = originalyear.ToString();
                updated = !string.Equals(orgValue, originalyear.ToString());
                return true;
            case "originaldate":
                orgValue = GetDictionaryValue(oldValues, "originaldate");
                track.AdditionalFields[GetFieldName(track,"originaldate")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "disc":
            case "disc number":
                orgValue = track.DiscNumber?.ToString() ?? string.Empty;
                if (!int.TryParse(value, out int disc))
                {
                    return false;
                }
                updated = track.DiscNumber != disc;
                track.DiscNumber = disc;
                return true;
            case "track number":
                orgValue = track.TrackNumber?.ToString() ?? string.Empty;
                if (!int.TryParse(value, out int trackNumber))
                {
                    return false;
                }
                updated = track.TrackNumber != trackNumber;
                track.TrackNumber = trackNumber;
                return true;
            case "total tracks":
                orgValue = track.TrackTotal?.ToString() ?? string.Empty;
                if (!int.TryParse(value, out int totalTracks))
                {
                    return false;
                }

                updated = track.TrackTotal != totalTracks;
                track.TrackTotal = totalTracks;
                return true;
            case "totaldiscs":
            case "total discs":
            case "disctotal":
                orgValue = track.DiscTotal?.ToString() ?? string.Empty;
                if (!int.TryParse(value, out int totalDiscs))
                {
                    return false;
                }

                updated = track.DiscTotal != totalDiscs;
                track.DiscTotal = totalDiscs;
                return true;
            case "musicbrainz artist id":
                orgValue = GetDictionaryValue(oldValues, "MusicBrainz Artist Id");
                track.AdditionalFields[GetFieldName(track,"MusicBrainz Artist Id")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "musicbrainz release group id":
                orgValue = GetDictionaryValue(oldValues, "MusicBrainz Release Group Id");
                track.AdditionalFields[GetFieldName(track,"MusicBrainz Release Group Id")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "musicbrainz release artist id":
                orgValue = GetDictionaryValue(oldValues, "MusicBrainz Release Artist Id");
                track.AdditionalFields[GetFieldName(track,"MusicBrainz Release Artist Id")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "musicbrainz release id":
                orgValue = GetDictionaryValue(oldValues, "MusicBrainz Release Id");
                track.AdditionalFields[GetFieldName(track,"MusicBrainz Release Id")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "musicbrainz release track id":
                orgValue = GetDictionaryValue(oldValues, "MusicBrainz Release Track Id");
                track.AdditionalFields[GetFieldName(track,"MusicBrainz Release Track Id")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "musicbrainz track id":
                orgValue = GetDictionaryValue(oldValues, "MusicBrainz Track Id");
                track.AdditionalFields[GetFieldName(track,"MusicBrainz Track Id")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "musicbrainz album artist id":
                orgValue = GetDictionaryValue(oldValues, "MusicBrainz Album Artist Id");
                track.AdditionalFields[GetFieldName(track,"MusicBrainz Album Artist Id")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "musicbrainz album id":
                orgValue = GetDictionaryValue(oldValues, "MusicBrainz Album Id");
                track.AdditionalFields[GetFieldName(track,"MusicBrainz Album Id")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "musicbrainz album type":
                orgValue = GetDictionaryValue(oldValues, "MusicBrainz Album Type");
                track.AdditionalFields[GetFieldName(track,"MusicBrainz Album Type")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "musicbrainz album release country":
                orgValue = GetDictionaryValue(oldValues, "MusicBrainz Album Release Country");
                track.AdditionalFields[GetFieldName(track,"MusicBrainz Album Release Country")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "musicbrainz album status":
                orgValue = GetDictionaryValue(oldValues, "MusicBrainz Album Status");
                track.AdditionalFields[GetFieldName(track,"MusicBrainz Album Status")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "script":
                orgValue = GetDictionaryValue(oldValues, "SCRIPT");
                track.AdditionalFields[GetFieldName(track,"SCRIPT")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "barcode":
                orgValue = GetDictionaryValue(oldValues, "BARCODE");
                track.AdditionalFields[GetFieldName(track, "BARCODE")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "media":
                orgValue = GetDictionaryValue(oldValues, "MEDIA");
                track.AdditionalFields[GetFieldName(track, "MEDIA")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "acoustid id":
                orgValue = GetDictionaryValue(oldValues, "Acoustid Id");
                track.AdditionalFields[GetFieldName(track, "Acoustid Id")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "acoustid fingerprint":
                orgValue = GetDictionaryValue(oldValues, "Acoustid Fingerprint");
                track.AdditionalFields[GetFieldName(track, "Acoustid Fingerprint")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "acoustid fingerprint duration":
                orgValue = GetDictionaryValue(oldValues, "Acoustid Fingerprint Duration");
                track.AdditionalFields[GetFieldName(track, "Acoustid Fingerprint Duration")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "isrc":
                orgValue = track.ISRC;
                updated = !string.Equals(track.ISRC, value);
                track.ISRC = value;
                return true;
            case "label":
                if (!string.Equals(value, "[no label]", StringComparison.OrdinalIgnoreCase))
                {
                    orgValue = GetDictionaryValue(oldValues, "LABEL");
                    track.AdditionalFields[GetFieldName(track, "LABEL")] = value;
                    updated = !string.Equals(orgValue, value);
                }
                return true;
            case "spotify track id":
                orgValue = GetDictionaryValue(oldValues, "Spotify Track Id");
                track.AdditionalFields[GetFieldName(track, "Spotify Track Id")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "spotify track explicit":
                orgValue = GetDictionaryValue(oldValues, "Spotify Track Explicit");
                track.AdditionalFields[GetFieldName(track, "Spotify Track Explicit")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "spotify track uri":
                orgValue = GetDictionaryValue(oldValues, "Spotify Track Uri");
                track.AdditionalFields[GetFieldName(track, "Spotify Track Uri")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "spotify track href":
                orgValue = GetDictionaryValue(oldValues, "Spotify Track Href");
                track.AdditionalFields[GetFieldName(track, "Spotify Track Href")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "spotify album id":
                orgValue = GetDictionaryValue(oldValues, "Spotify Album Id");
                track.AdditionalFields[GetFieldName(track, "Spotify Album Id")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "spotify album group":
                orgValue = GetDictionaryValue(oldValues, "Spotify Album Group");
                track.AdditionalFields[GetFieldName(track, "Spotify Album Group")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "spotify album release date":
                orgValue = GetDictionaryValue(oldValues, "Spotify Album Release Date");
                track.AdditionalFields[GetFieldName(track, "Spotify Album Release Date")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "spotify artist href":
                orgValue = GetDictionaryValue(oldValues, "Spotify Artist Href");
                track.AdditionalFields[GetFieldName(track, "Spotify Artist Href")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "spotify artist genres":
                orgValue = GetDictionaryValue(oldValues, "Spotify Artist Genres");
                track.AdditionalFields[GetFieldName(track, "Spotify Artist Genres")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "spotify artist id":
                orgValue = GetDictionaryValue(oldValues, "Spotify Artist Id");
                track.AdditionalFields[GetFieldName(track, "Spotify Artist Id")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "upc":
                orgValue = GetDictionaryValue(oldValues, "UPC");
                track.AdditionalFields[GetFieldName(track, "UPC")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "genre":
                orgValue = GetDictionaryValue(oldValues, "genre");
                track.AdditionalFields[GetFieldName(track, "genre")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "tidal track id":
                orgValue = GetDictionaryValue(oldValues, "Tidal Track Id");
                track.AdditionalFields[GetFieldName(track, "Tidal Track Id")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "tidal track explicit":
                orgValue = GetDictionaryValue(oldValues, "Tidal Track Explicit");
                track.AdditionalFields[GetFieldName(track, "Tidal Track Explicit")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "tidal track href":
                orgValue = GetDictionaryValue(oldValues, "Tidal Track Href");
                track.AdditionalFields[GetFieldName(track, "Tidal Track Href")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "tidal album id":
                orgValue = GetDictionaryValue(oldValues, "Tidal Album Id");
                track.AdditionalFields[GetFieldName(track, "Tidal Album Id")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "tidal album href":
                orgValue = GetDictionaryValue(oldValues, "Tidal Album Href");
                track.AdditionalFields[GetFieldName(track, "Tidal Album Href")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "tidal album release date":
                orgValue = GetDictionaryValue(oldValues, "Tidal Album Release Date");
                track.AdditionalFields[GetFieldName(track, "Tidal Album Release Date")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "tidal artist id":
                orgValue = GetDictionaryValue(oldValues, "Tidal Artist Id");
                track.AdditionalFields[GetFieldName(track, "Tidal Artist Id")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "tidal artist href":
                orgValue = GetDictionaryValue(oldValues, "Tidal Artist Href");
                track.AdditionalFields[GetFieldName(track, "Tidal Artist Href")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "copyright":
                orgValue = GetDictionaryValue(oldValues, "Copyright");
                track.Copyright = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "deezer track id":
                orgValue = GetDictionaryValue(oldValues, "Deezer Track Id");
                track.AdditionalFields[GetFieldName(track, "Deezer Track Id")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "deezer track explicit":
                orgValue = GetDictionaryValue(oldValues, "Deezer Track Explicit");
                track.AdditionalFields[GetFieldName(track, "Deezer Track Explicit")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "deezer track href":
                orgValue = GetDictionaryValue(oldValues, "Deezer Track Href");
                track.AdditionalFields[GetFieldName(track, "Deezer Track Href")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "deezer album id":
                orgValue = GetDictionaryValue(oldValues, "Deezer Album Id");
                track.AdditionalFields[GetFieldName(track, "Deezer Album Id")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "deezer album href":
                orgValue = GetDictionaryValue(oldValues, "Deezer Album Href");
                track.AdditionalFields[GetFieldName(track, "Deezer Album Href")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "deezer album release date":
                orgValue = GetDictionaryValue(oldValues, "Deezer Album Release Date");
                track.AdditionalFields[GetFieldName(track, "Deezer Album Release Date")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "deezer artist id":
                orgValue = GetDictionaryValue(oldValues, "Deezer Artist Id");
                track.AdditionalFields[GetFieldName(track, "Deezer Artist Id")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
            case "deezer artist href":
                orgValue = GetDictionaryValue(oldValues, "Deezer Artist Href");
                track.AdditionalFields[GetFieldName(track, "Deezer Artist Href")] = value;
                updated = !string.Equals(orgValue, value);
                return true;
        }

        return false;
    }

    private bool IsDictionaryUpdated(Dictionary<string, string> mediaTags, 
        Dictionary<string, string> oldValues,
        string tagName)
    {
        string fieldName = GetFieldName(mediaTags, tagName);

        if (mediaTags.ContainsKey(fieldName) &&
            !oldValues.ContainsKey(fieldName))
        {
            return true;
        }
        
        return !string.Equals(mediaTags[GetFieldName(mediaTags, fieldName)], oldValues[GetFieldName(mediaTags, fieldName)]);
    }
    private bool IsDictionaryUpdated(Track track, 
        Dictionary<string, string> oldValues,
        string tagName)
    {
        string fieldName = GetFieldName(track, tagName);

        if (track.AdditionalFields.ContainsKey(fieldName) &&
            !oldValues.ContainsKey(fieldName))
        {
            return true;
        }
        
        return !string.Equals(track.AdditionalFields[GetFieldName(track, fieldName)], oldValues[GetFieldName(track, fieldName)]);
    }

    public string GetDictionaryValue(Dictionary<string, string> mediaTags, string fieldName)
    {
        fieldName = GetFieldName(mediaTags, fieldName);
        if (mediaTags.TryGetValue(fieldName, out string value))
        {
            return value;
        }
        return string.Empty;
    }
    public string GetDictionaryValue(Track track, string fieldName)
    {
        fieldName = GetFieldName(track, fieldName);
        if (track.AdditionalFields.TryGetValue(fieldName, out string value))
        {
            return value;
        }
        return string.Empty;
    }
    
    public string GetFieldName(Dictionary<string, string> mediaTags, string field)
    {
        if (mediaTags.Keys.Any(key => key.ToLower() == field.ToLower()))
        {
            return mediaTags.First(pair => pair.Key.ToLower() == field.ToLower()).Key;
        }
        return field;
    }
    public string GetFieldName(Track track, string field)
    {
        if (track.AdditionalFields.Keys.Any(key => key.ToLower() == field.ToLower()))
        {
            return track.AdditionalFields.First(pair => pair.Key.ToLower() == field.ToLower()).Key;
        }
        return field;
    }

    public string GetTagValue(Dictionary<string, string> mediaTags, string tagName)
    {
        string fieldName = GetFieldName(mediaTags, tagName);
        if (mediaTags.ContainsKey(fieldName))
        {
            return mediaTags[fieldName];
        }

        return string.Empty;
    }
    public string GetTagValue(Track track, string tagName)
    {
        string fieldName = GetFieldName(track, tagName);
        if (track.AdditionalFields.ContainsKey(fieldName))
        {
            return track.AdditionalFields[fieldName];
        }

        return string.Empty;
    }
    
    public async Task<bool> SafeSaveAsync(Track track)
    {
        FileInfo targetFile = new FileInfo(track.Path);
        string tempFile = $"{track.Path}.tmp{targetFile.Extension}";
        
        if (File.Exists(tempFile))
        {
            File.Delete(tempFile);
        }
        
        bool success = false;
        try
        {
            success = await track.SaveToAsync(tempFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        if (success && File.Exists(tempFile))
        {
            File.Move(tempFile, targetFile.FullName, true);
        }
        else if (File.Exists(tempFile))
        {
            File.Delete(tempFile);
        }

        return success;
    }
    
    public void UpdateTag(Track track,
        MetadataInfo metadataInfo,
        string tagName, 
        string? value, 
        ref bool trackInfoUpdated, 
        bool overwriteTagValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (int.TryParse(value, out int intValue) && intValue == 0)
        {
            return;
        }
        
        tagName = GetFieldName(metadataInfo.MediaTags, tagName);
        value = _normalizerService.ReplaceInvalidCharacters(value);
        
        if (!overwriteTagValue &&
            metadataInfo.MediaTags.ContainsKey(tagName) &&
            !string.IsNullOrWhiteSpace(metadataInfo.MediaTags[tagName]))
        {
            return;
        }
        
        string orgValue = string.Empty;
        bool tempIsUpdated = false;
        UpdateTrackTag(track, tagName, value, ref tempIsUpdated, ref orgValue, metadataInfo);

        //double check incase the same check above somehow failed (because of tag typos etc)
        //write back the original value
        if (!overwriteTagValue && tempIsUpdated && !string.IsNullOrWhiteSpace(orgValue))
        {
            UpdateTrackTag(track, tagName, orgValue, ref tempIsUpdated, ref orgValue, metadataInfo);
        }
        
        if (tempIsUpdated && !string.Equals(orgValue, value))
        {
            Console.WriteLine($"Updating tag '{tagName}' value '{orgValue}' =>  '{value}'");
            trackInfoUpdated = true;
        }
    }
}