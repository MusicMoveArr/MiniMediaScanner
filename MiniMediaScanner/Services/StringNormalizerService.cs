using System.Globalization;
using System.Text.RegularExpressions;

namespace MiniMediaScanner.Services;

public class StringNormalizerService
{
    public string NormalizeText(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }
        
        //replace special characters
        input = input.Replace('–', '-'); //en dash -> hypen
        input = input.Replace('—', '-'); //em dash -> hypen
        input = input.Replace("…", "..."); //horizontal ellipsis -> 3 dots
            
        // Words to exclude from capitalization (except if they're the first word)
        HashSet<string> smallWords = new HashSet<string> { "of", "the", "and", "in", "on", "at", "for", "to", "a" };

        // Create a TextInfo object for title casing
        TextInfo textInfo = CultureInfo.GetCultureInfo("en-US").TextInfo;

        // Split the string into words and delimiters
        var words = new List<string>();
        var delimiters = new List<char>();
        char[] separatorCharacters = { ':', '-', '_', ' ', '/', ',', '(', ')', '[', ']' }; // Add more as needed

        int start = 0;
        for (int i = 0; i < input.Length; i++)
        {
            if (Array.Exists(separatorCharacters, c => c == input[i]))
            {
                // Add word and delimiter
                if (start < i)
                {
                    words.Add(input.Substring(start, i - start)); // Add word
                }
                delimiters.Add(input[i]); // Add delimiter
                start = i + 1;
            }
        }

        // Add the last word if any
        if (start < input.Length)
        {
            words.Add(input.Substring(start));
        }

        // Capitalize each word considering small words
        for (int i = 0; i < words.Count; i++)
        {
            string word = words[i];
            
            //skip possible roman letters
            if (Regex.IsMatch(word, @"^[IVXLCDM]+$", RegexOptions.IgnoreCase))
            {
                continue;
            }
            
            word = words[i].ToLower();
            if (i == 0 || !smallWords.Contains(word)) // Capitalize if first word or not a small word
            {
                words[i] = CapitalizeFirstChar(word);
            }
            else if (smallWords.Contains(word))
            {
                words[i] = word;
            }
        }

        // Reconstruct the string with original delimiters
        string result = "";
        int wordIndex = 0, delimiterIndex = 0;

        for (int i = 0; i < input.Length; i++)
        {
            if (delimiterIndex < delimiters.Count && input[i] == delimiters[delimiterIndex])
            {
                result += delimiters[delimiterIndex++];
            }
            else if (wordIndex < words.Count)
            {
                result += words[wordIndex++];
                i += words[wordIndex - 1].Length - 1; // Skip processed word
            }
        }

        return result;
    }
    
    public string CapitalizeFirstChar(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        
        char firstChar = char.ToUpper(input[0]);
        string restOfString = input.Length > 1 ? input.Substring(1) : string.Empty;

        return firstChar + restOfString;
    }
    
    public string ReplaceInvalidCharacters(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }
        return value.Replace("\0", string.Empty)
                    .Replace("\\u0000", string.Empty)
                    .Replace("\\u0001", string.Empty);
    }
}