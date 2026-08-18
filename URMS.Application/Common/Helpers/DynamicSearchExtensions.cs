using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace URMS.Application.Common.Helpers;

/// <summary>
/// Generic dynamic search extension for IQueryable (SQL-translatable) and IEnumerable (in-memory).
/// Automatically discovers DTO/entity properties via cached reflection and builds
/// Expression Trees that EF Core translates to SQL WHERE clauses.
///
/// Usage:
///   query.ApplySearch("name", "أحمد")        → searches name-matching properties
///   query.ApplySearch(null, "أحمد")           → searches all string properties
///   query.ApplySearch("isRegistered", "true") → filters by bool property
///
/// Adding a new property to any DTO → search automatically discovers it. Zero code changes.
/// </summary>
public static class DynamicSearchExtensions
{
    // ─── Caches ───
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo[]> _columnCache = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _stringPropsCache = new();

    private static readonly MethodInfo StringContainsMethod =
        typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

    // ═══════════════════════════════════════════════════════════════
    // IQueryable<T> — Translates to SQL WHERE via Expression Trees
    // ═══════════════════════════════════════════════════════════════

    public static IQueryable<T> ApplySearch<T>(
        this IQueryable<T> source, string? searchColumn, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return source;

        var term = searchTerm.Trim();

        var predicate = !string.IsNullOrWhiteSpace(searchColumn)
            ? BuildColumnPredicate<T>(searchColumn.Trim(), term)
            : BuildGlobalPredicate<T>(term);

        return predicate != null ? source.Where(predicate) : source;
    }

    // ═══════════════════════════════════════════════════════════════
    // IEnumerable<T> — Backward compatibility for in-memory collections
    // ═══════════════════════════════════════════════════════════════

    public static IEnumerable<T> ApplySearch<T>(
        this IEnumerable<T> source, string? searchColumn, string? searchTerm)
    {
        if (source is IQueryable<T> queryable)
            return queryable.ApplySearch(searchColumn, searchTerm);

        if (string.IsNullOrWhiteSpace(searchTerm))
            return source;

        var term = searchTerm.Trim();

        var predicate = !string.IsNullOrWhiteSpace(searchColumn)
            ? BuildColumnPredicate<T>(searchColumn.Trim(), term)
            : BuildGlobalPredicate<T>(term);

        return predicate != null ? source.AsQueryable().Where(predicate) : source;
    }

    // ═══════════════════════════════════════════════════════════════
    // Expression Tree Builders
    // ═══════════════════════════════════════════════════════════════

    private static Expression<Func<T, bool>>? BuildColumnPredicate<T>(string column, string term)
    {
        var props = ResolveProperties<T>(column);
        return props.Length > 0 ? CombineMatches<T>(props, term) : null;
    }

    private static Expression<Func<T, bool>>? BuildGlobalPredicate<T>(string term)
    {
        var props = _stringPropsCache.GetOrAdd(typeof(T), t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
             .Where(p => p.CanRead && p.PropertyType == typeof(string))
             .ToArray());

        return props.Length > 0 ? CombineMatches<T>(props, term) : null;
    }

    /// <summary>
    /// Combines multiple property checks into a single OR predicate.
    /// Example output: x => x.NameAr.Contains(term) || x.NameEn.Contains(term)
    /// </summary>
    private static Expression<Func<T, bool>>? CombineMatches<T>(PropertyInfo[] props, string term)
    {
        var param = Expression.Parameter(typeof(T), "x");
        Expression? combined = null;

        foreach (var prop in props)
        {
            var match = BuildPropertyMatch(param, prop, term);
            if (match == null) continue;
            combined = combined == null ? match : Expression.OrElse(combined, match);
        }

        return combined != null
            ? Expression.Lambda<Func<T, bool>>(combined, param)
            : null;
    }

    /// <summary>
    /// Builds a match expression for a single property.
    /// String  → x.Prop != null && x.Prop.Contains(term)  →  SQL: Prop LIKE '%term%'
    /// Bool    → x.Prop == parsedBool                      →  SQL: Prop = 1/0
    /// </summary>
    private static Expression? BuildPropertyMatch(ParameterExpression param, PropertyInfo prop, string term)
    {
        var access = Expression.Property(param, prop);
        var underlying = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

        if (underlying == typeof(string))
        {
            var notNull = Expression.NotEqual(access, Expression.Constant(null, typeof(string)));
            var contains = Expression.Call(access, StringContainsMethod, Expression.Constant(term));
            return Expression.AndAlso(notNull, contains);
        }

        if (underlying == typeof(bool))
        {
            var boolVal = ParseBool(term);
            if (boolVal == null) return null;

            if (prop.PropertyType == typeof(bool?))
            {
                var hasValue = Expression.Property(access, "HasValue");
                var value = Expression.Property(access, "Value");
                return Expression.AndAlso(hasValue,
                    Expression.Equal(value, Expression.Constant(boolVal.Value)));
            }

            return Expression.Equal(access, Expression.Constant(boolVal.Value));
        }

        return null;
    }

    // ═══════════════════════════════════════════════════════════════
    // Property Resolution (Cached)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Resolves which properties match a search column name.
    /// Priority: exact match → normalized match → partial match.
    /// Results are cached per (Type, columnName) pair.
    /// </summary>
    private static PropertyInfo[] ResolveProperties<T>(string column)
    {
        return _columnCache.GetOrAdd((typeof(T), NormalizeName(column)), _ =>
        {
            var all = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToArray();

            var norm = NormalizeName(column);

            // 1. Exact match (case-insensitive)
            var exact = all.Where(p =>
                string.Equals(p.Name, column, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (exact.Length > 0) return exact;

            // 2. Normalized exact match ("isregistered" == "IsRegistered")
            var normExact = all.Where(p => NormalizeName(p.Name) == norm).ToArray();
            if (normExact.Length > 0) return normExact;

            // 3. Partial match ("name" → "StudentNameAr", "StudentNameEn")
            var partial = all.Where(p => NormalizeName(p.Name).Contains(norm)).ToArray();
            return partial;
        });
    }

    private static string NormalizeName(string name) =>
        name.Replace("_", "").Replace("-", "").ToLowerInvariant();

    private static bool? ParseBool(string term)
    {
        if (bool.TryParse(term, out var b)) return b;
        return term switch
        {
            "1" or "مسجل" or "نعم" => true,
            "0" or "غير مسجل" or "لا" => false,
            _ => null
        };
    }
}
