using System.IO;
using System.Text.Json;

namespace Prononce.Services;

/// <summary>
/// Instant offline dictionary lookup service for French words and common expressions.
/// Works 100% offline with zero cloud latency.
/// </summary>
public sealed class DictionaryService
{
    public static DictionaryService Shared { get; } = new();

    private readonly Dictionary<string, string> _lexicon = new(StringComparer.OrdinalIgnoreCase);
    private bool _isLoaded;

    private DictionaryService()
    {
        LoadLexicon();
    }

    private void LoadLexicon()
    {
        if (_isLoaded) return;

        try
        {
            // Look for dictionary.json in output directory or Resources folder
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var pathsToTry = new[]
            {
                Path.Combine(baseDir, "Resources", "dictionary.json"),
                Path.Combine(baseDir, "dictionary.json"),
                Path.Combine(AppContext.BaseDirectory, "Resources", "dictionary.json")
            };

            string? foundPath = pathsToTry.FirstOrDefault(File.Exists);

            if (foundPath != null)
            {
                var json = File.ReadAllText(foundPath);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (dict != null)
                {
                    foreach (var kvp in dict)
                    {
                        _lexicon[kvp.Key.Trim()] = kvp.Value.Trim();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load dictionary: {ex.Message}");
        }

        _isLoaded = true;
    }

    /// <summary>
    /// Looks up a French word, phrase, or sentence in the offline dictionary.
    /// </summary>
    public string? Lookup(string phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase)) return null;

        var cleaned = CleanPhrase(phrase);
        if (string.IsNullOrEmpty(cleaned)) return null;

        // 1. Direct exact lookup
        if (_lexicon.TryGetValue(cleaned, out var definition))
        {
            return definition;
        }

        // 2. Inflection fallback (remove trailing 's' for plurals or 'e' for feminine)
        if (cleaned.EndsWith("s", StringComparison.OrdinalIgnoreCase) && cleaned.Length > 3)
        {
            var singular = cleaned.Substring(0, cleaned.Length - 1);
            if (_lexicon.TryGetValue(singular, out var singularDef))
            {
                return singularDef;
            }
        }

        if (cleaned.EndsWith("e", StringComparison.OrdinalIgnoreCase) && cleaned.Length > 3)
        {
            var masc = cleaned.Substring(0, cleaned.Length - 1);
            if (_lexicon.TryGetValue(masc, out var mascDef))
            {
                return mascDef;
            }
        }

        // 3. Multi-word phrase: translate first recognizable key word or prefix
        var words = cleaned.Split(new[] { ' ', '\'', '’' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 1)
        {
            var translatedParts = new List<string>();
            foreach (var word in words.Take(4))
            {
                if (_lexicon.TryGetValue(word, out var def))
                {
                    // Take first definition term
                    var term = def.Split(',', ';', '/')[0].Trim();
                    translatedParts.Add($"{word} ({term})");
                }
            }

            if (translatedParts.Count > 0)
            {
                return string.Join(" • ", translatedParts);
            }
        }

        return null;
    }

    private static string CleanPhrase(string raw)
    {
        var trimmed = raw.Trim();
        // Strip surrounding quotation marks or common punctuation
        trimmed = trimmed.Trim('"', '\'', '«', '»', '.', ',', '!', '?', ':', ';', '(', ')');
        return trimmed.ToLowerInvariant();
    }
}
