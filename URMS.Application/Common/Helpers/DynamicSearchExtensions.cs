using System.Reflection;

namespace URMS.Application.Common.Helpers;

/// <summary>
/// Dynamic search and filter helper for in-memory collections.
/// Automatically matches search columns against DTO properties using Reflection.
/// </summary>
public static class DynamicSearchExtensions
{
    public static IEnumerable<T> ApplySearch<T>(
        this IEnumerable<T> source,
        string? searchColumn,
        string? searchTerm)
    {
        if (source is null || string.IsNullOrWhiteSpace(searchTerm))
            return source ?? Enumerable.Empty<T>();

        var term = searchTerm.Trim();

        // 1. If a specific column is requested, find matching properties
        if (!string.IsNullOrWhiteSpace(searchColumn))
        {
            var matchingProps = FindMatchingProperties<T>(searchColumn.Trim());
            if (matchingProps.Any())
            {
                return source.Where(item => matchingProps.Any(prop => MatchesProperty(item, prop, term)));
            }
        }

        // 2. Fallback: Search across all readable string properties
        var stringProps = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.PropertyType == typeof(string))
            .ToList();

        return source.Where(item =>
            stringProps.Any(p =>
                p.GetValue(item) is string val &&
                val.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }

    private static List<PropertyInfo> FindMatchingProperties<T>(string columnName)
    {
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToList();

        var normalizedCol = NormalizeName(columnName);

        // 1. Exact match (case insensitive)
        var exact = props.Where(p => string.Equals(p.Name, columnName, StringComparison.OrdinalIgnoreCase)).ToList();
        if (exact.Any()) return exact;

        // 2. Normalized exact match (e.g. "isregistered" == "isregistered")
        var normExact = props.Where(p => NormalizeName(p.Name) == normalizedCol).ToList();
        if (normExact.Any()) return normExact;

        // 3. Partial match (e.g. "name" matches "FullNameAr" & "FullNameEn", "code" matches "UniversityCode")
        var partial = props.Where(p => NormalizeName(p.Name).Contains(normalizedCol)).ToList();
        if (partial.Any()) return partial;

        return new List<PropertyInfo>();
    }

    private static string NormalizeName(string name)
    {
        return name.Replace("_", "")
                   .Replace("-", "")
                   .ToLowerInvariant();
    }

    private static bool MatchesProperty<T>(T item, PropertyInfo prop, string term)
    {
        var value = prop.GetValue(item);
        if (value is null) return false;

        var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

        if (targetType == typeof(bool))
        {
            if (bool.TryParse(term, out bool boolVal))
                return (bool)value == boolVal;

            if (term == "1" || term == "0")
                return (bool)value == (term == "1");

            if (string.Equals(term, "نعم", StringComparison.OrdinalIgnoreCase) || string.Equals(term, "مسجل", StringComparison.OrdinalIgnoreCase))
                return (bool)value;

            if (string.Equals(term, "لا", StringComparison.OrdinalIgnoreCase) || string.Equals(term, "غير مسجل", StringComparison.OrdinalIgnoreCase))
                return !(bool)value;

            return false;
        }

        if (targetType == typeof(string))
        {
            return ((string)value).Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        return value.ToString()?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false;
    }
}
