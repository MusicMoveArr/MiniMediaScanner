using ATL;
using FFMpegCore;

namespace MiniMediaScanner.Services;

public class MediaTagWriteService
{
    public bool Save(FileInfo targetFile, string artistName, string albumName, string title)
    {
        if (title.Contains("\""))
        {
            title = title.Replace("\"", "\"\"");
        }        
        string tempFile = $"{targetFile.FullName}.tmp{targetFile.Extension}";
        bool success = false;
        try
        {
            success = FFMpegArguments
                .FromFileInput(targetFile.FullName)
                .OutputToFile(tempFile, overwrite: true, options => options
                    .WithCustomArgument($"-metadata album_artist=\"{artistName}\"")
                    .WithCustomArgument($"-metadata artist=\"{artistName}\"")
                    .WithCustomArgument($"-metadata album=\"{albumName}\"")
                    .WithCustomArgument($"-metadata title=\"{title}\"")
                    .WithCustomArgument("-codec copy")) // Prevents re-encoding
                .ProcessSynchronously();
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

    public bool UpdateTrackTag(Track track, string tag, string value)
    {
        switch (tag.ToLower())
        {
            case "date":
                track.AdditionalFields[GetFieldName(track,"date")] = value;
                return true;
            case "catalognumber":
                track.CatalogNumber = value;
                return true;
            case "asin":
                track.AdditionalFields[GetFieldName(track,"asin")] = value;
                return true;
            case "year":
                if (!int.TryParse(value, out int year))
                {
                    return false;
                }
                track.Year = year;
                return true;
            case "originalyear":
                if (!int.TryParse(value, out int originalyear))
                {
                    return false;
                }
                track.AdditionalFields[GetFieldName(track,"originalyear")] = originalyear.ToString();
                return true;
            case "originaldate":
                track.AdditionalFields[GetFieldName(track,"originaldate")] = value;
                return true;
            case "disc":
            case "disc number":
                if (!int.TryParse(value, out int disc))
                {
                    return false;
                }
                track.DiscNumber = disc;
                return true;
            case "track number":
                if (!int.TryParse(value, out int trackNumber))
                {
                    return false;
                }
                track.TrackNumber = trackNumber;
                return true;
            case "total tracks":
                if (!int.TryParse(value, out int totalTracks))
                {
                    return false;
                }
                track.TrackTotal = totalTracks;
                return true;
            case "musicbrainz artist id":
                track.AdditionalFields[GetFieldName(track,"MusicBrainz Artist Id")] = value;
                return true;
            case "musicbrainz release group id":
                track.AdditionalFields[GetFieldName(track,"MusicBrainz Release Group Id")] = value;
                return true;
            case "musicbrainz release artist id":
                track.AdditionalFields[GetFieldName(track,"MusicBrainz Release Artist Id")] = value;
                return true;
            case "musicbrainz release id":
                track.AdditionalFields[GetFieldName(track,"MusicBrainz Release Id")] = value;
                return true;
            case "musicbrainz track id":
                track.AdditionalFields[GetFieldName(track,"MusicBrainz Track Id")] = value;
                return true;
            case "musicbrainz album artist id":
                track.AdditionalFields[GetFieldName(track,"MusicBrainz Album Artist Id")] = value;
                return true;
            case "musicbrainz album id":
                track.AdditionalFields[GetFieldName(track,"MusicBrainz Album Id")] = value;
                return true;
            case "musicbrainz album type":
                track.AdditionalFields[GetFieldName(track,"MusicBrainz Album Type")] = value;
                return true;
            case "musicbrainz album release country":
                track.AdditionalFields[GetFieldName(track,"MusicBrainz Album Release Country")] = value;
                return true;
            case "musicbrainz album status":
                track.AdditionalFields[GetFieldName(track,"MusicBrainz Album Status")] = value;
                return true;
            case "script":
                track.AdditionalFields[GetFieldName(track,"SCRIPT")] = value;
                return true;
            case "barcode":
                track.AdditionalFields[GetFieldName(track, "BARCODE")] = value;
                return true;
            case "media":
                track.AdditionalFields[GetFieldName(track, "MEDIA")] = value;
                return true;
            case "acoustid id":
                track.AdditionalFields[GetFieldName(track, "Acoustid Id")] = value;
                return true;
        }

        return false;
    }
    
    public string GetFieldName(Track track, string field)
    {
        if (track.AdditionalFields.Keys.Any(key => key.ToLower() == field.ToLower()))
        {
            return track.AdditionalFields.First(pair => pair.Key.ToLower() == field.ToLower()).Key;
        }

        return field;
    }
    
    public bool SaveTag(FileInfo targetFile, string tag, string value)
    {
        Track track = new Track(targetFile.FullName);
        UpdateTrackTag(track, tag, value);

        return SafeSave(track);
    }
    
    public bool SafeSave(Track track)
    {
        FileInfo targetFile = new FileInfo(track.Path);
        string tempFile = $"{track.Path}.tmp{targetFile.Extension}";
        bool success = false;
        try
        {
            success = track.SaveTo(tempFile);
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
}