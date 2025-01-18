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
        track.AdditionalFields = track.AdditionalFields.ToDictionary(StringComparer.OrdinalIgnoreCase);

        switch (tag.ToLower())
        {
            case "date":
                track.AdditionalFields["date"] = value;
                return true;
            case "catalognumber":
                track.CatalogNumber = value;
                return true;
            case "asin":
                track.AdditionalFields["asin"] = value;
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
                track.AdditionalFields["originalyear"] = originalyear.ToString();
                return true;
            case "originaldate":
                track.AdditionalFields["originaldate"] = value;
                return true;
            case "disc":
                if (!int.TryParse(value, out int disc))
                {
                    return false;
                }
                track.DiscNumber = disc;
                return true;
        }

        return false;
    }
    
    public bool SaveTag(FileInfo targetFile, string tag, string value)
    {
        string tempFile = $"{targetFile.FullName}.tmp{targetFile.Extension}";
        bool success = false;
        try
        {
            Track track = new Track(targetFile.FullName);
            UpdateTrackTag(track, tag, value);

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