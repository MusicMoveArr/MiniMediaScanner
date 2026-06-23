namespace MiniMediaScanner.Helpers;

public class PagedReadLineHelper
{
    public static async Task ReadLinesAsync(
        string filePathSource,
        Func<string, int, Task> asyncAction)
    {
        int readLineCount = 0;
        const int bulkRead = 1000;
        using FileStream fStream  = File.OpenRead(filePathSource);
        using BufferedStream bs = new BufferedStream(fStream);
        using StreamReader sr = new StreamReader(bs);

        List<string> lines = new List<string>();

        while ((lines = await ReadLinesAsync(sr, bulkRead)).Any())
        {
            readLineCount += lines.Count;
            foreach (string line in lines)
            {
                try
                {
                    await asyncAction(line, readLineCount);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message + "\r\n" + e.StackTrace);
                }
            }
        }
    }

    private static async Task<List<string>> ReadLinesAsync(StreamReader sr, int count)
    {
        var lines = new List<string>();
        string? line;
        while (lines.Count < count && (line = await sr.ReadLineAsync()) != null)
        {
            lines.Add(line);
        }
        return lines;
    }
}