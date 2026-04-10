namespace MiniMediaScanner.Helpers;

public static class StringHelper
{
    public static string CleanupInvalidChars(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }
        
        var invalidChars = new HashSet<char>
        {
            '\uFFFE',  // 0xFFFE - reversed BOM
            '\uFEFF',  // 0xFEFF - BOM / zero-width no-break space
            '\uFFFD',  // 0xFFFD - replacement character
            '\u0000',  // 0x0000 - null character
            '\u0001',  // 0x0000 - null character
            ' '
        };
        if (invalidChars.Any(c => value.Contains(c)))
        {
            foreach(var c in invalidChars)
            {
                value = value.Replace(c.ToString(), string.Empty);
            }
        }
        
        return value;
    }
}