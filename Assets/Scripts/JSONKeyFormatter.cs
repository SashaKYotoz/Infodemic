using SimpleJSON;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections.Generic;

public static class JsonKeyFormatter
{
    public static string FormatJsonKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        // Split by uppercase letters
        string[] words = Regex.Split(key, @"(?<!^)(?=[A-Z])");

        // Process each word
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(words[i].ToLower());
            }
        }

        return string.Join(" ", words);
    }

    public static List<string> GetFormattedKeysFromJson(string jsonString)
    {
        var formattedKeys = new List<string>();
        var jsonNode = JSON.Parse(jsonString);
        
        // If it's an object, process its keys
        if (jsonNode.IsObject)
        {
            foreach (var kvp in jsonNode.AsObject)
            {
                formattedKeys.Add(FormatJsonKey(kvp.Key));
            }
        }

        return formattedKeys;
    }
}