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
}