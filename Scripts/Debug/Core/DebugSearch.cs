#if UNITY_EDITOR || EIGHTAID_TEST_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>コマンド・シナリオ・候補に共通の、正規化した複数語AND検索。</summary>
public sealed class DebugSearchQuery
{
    private static readonly Dictionary<string, string> Synonyms = new Dictionary<string, string>(StringComparer.Ordinal);
    private readonly string[] _terms;
    public bool IsEmpty => _terms.Length == 0;

    public DebugSearchQuery(string text)
    {
        _terms = Normalize(text).Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();
    }

    public static void RegisterSynonyms(string canonical, params string[] synonyms)
    {
        foreach (string synonym in synonyms)
        {
            Synonyms[synonym.Normalize(NormalizationForm.FormKC).ToLowerInvariant()] = canonical.ToLowerInvariant();
        }
    }

    public static string Normalize(string text)
    {
        string normalized = (text ?? string.Empty).Normalize(NormalizationForm.FormKC).ToLowerInvariant();
        foreach (var pair in Synonyms.OrderByDescending(pair => pair.Key.Length))
        {
            normalized = normalized.Replace(pair.Key, pair.Value);
        }
        return normalized;
    }

    public int Score(string title, params string[] fields)
    {
        if (IsEmpty) return 0;
        string normalizedTitle = Normalize(title);
        string[] normalizedFields = fields.Select(Normalize).ToArray();
        int score = 0;
        foreach (string term in _terms)
        {
            if (normalizedTitle.Contains(term)) score += 10;
            else if (normalizedFields.Any(field => field.Contains(term))) score += 1;
            else return -1;
        }
        if (_terms.Length == 1 && normalizedTitle == _terms[0]) score += 100;
        return score;
    }

    public string Explain(string title, params string[] fields)
    {
        if (IsEmpty || _terms.All(term => Normalize(title).Contains(term))) return string.Empty;
        string field = fields.FirstOrDefault(value => _terms.Any(term => Normalize(value).Contains(term)));
        if (string.IsNullOrEmpty(field)) return string.Empty;
        return "一致: " + (field.Length > 72 ? field.Substring(0, 72) + "…" : field);
    }
}

public static class DebugCategory
{
    public static string Group(string category)
    {
        int separator = (category ?? string.Empty).IndexOf(" / ", StringComparison.Ordinal);
        return separator < 0 ? category ?? string.Empty : category.Substring(0, separator);
    }

    public static bool Matches(string category, string selected)
    {
        return string.IsNullOrEmpty(selected) || category == selected ||
            (category ?? string.Empty).StartsWith(selected + " / ", StringComparison.Ordinal);
    }
}
#endif
