using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Reads the analyzer's <c>.editorconfig</c> / MSBuild-property surface via
/// <see cref="AnalyzerConfigOptions"/>. Lookups are per-syntax-tree, so a
/// directory-scoped <c>.editorconfig</c> section gets its own target set free.
/// </summary>
internal static class AnalyzerConfigResolver
{
    public static readonly TargetDbms[] AllDbms =
    [
        TargetDbms.MySql, TargetDbms.Oracle, TargetDbms.PostgreSql, TargetDbms.Sqlite, TargetDbms
            .SqlServer,
    ];

    private static readonly Dictionary<TargetDbms, string> SyntaxDbmsNames = new()
    {
        [TargetDbms.MySql] = "mysql",
        [TargetDbms.Oracle] = "oracle",
        [TargetDbms.PostgreSql] = "postgresql",
        [TargetDbms.Sqlite] = "sqlite",
        [TargetDbms.SqlServer] = "sqlserver",
    };

    public const string SyntaxKeyPrefix = "sqlartisan_syntax_";
    public const string AnyValue = "any";
    public const string NoneValue = "none";

    public static string SyntaxKey(TargetDbms dbms) => SyntaxKeyPrefix + SyntaxDbmsNames[dbms];

    /// <summary>
    /// The MSBuild-property fallback for <see cref="SyntaxKey"/>, populated via
    /// the <c>CompilerVisibleProperty</c> entries in
    /// src/SqlArtisan.Analyzers/build/SqlArtisan.props.
    /// </summary>
    public static string SyntaxMSBuildPropertyKey(TargetDbms dbms) =>
        $"build_property.SqlArtisanSyntax{dbms}";

    public static bool IsRecognizedSyntaxValue(string value) =>
        string.Equals(value, AnyValue, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, NoneValue, StringComparison.OrdinalIgnoreCase)
        || EngineVersion.TryParse(value, out _);

    /// <summary>
    /// Whether <paramref name="key"/> is one of the five <c>sqlartisan_syntax_&lt;dbms&gt;</c>
    /// keys, any casing.
    /// </summary>
    public static bool IsRecognizedSyntaxKey(string key)
    {
        foreach (TargetDbms dbms in AllDbms)
        {
            if (string.Equals(key, SyntaxKey(dbms), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether any <c>sqlartisan_syntax_*</c> key carries a value, on either
    /// surface — recognized or not.
    /// </summary>
    public static bool IsFamilyPresent(AnalyzerConfigOptions options)
    {
        foreach (TargetDbms dbms in AllDbms)
        {
            if (IsFamilyKeySet(options, dbms))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether the family names <paramref name="dbms"/> on either surface, with any non-blank
    /// value.
    /// </summary>
    public static bool IsFamilyKeySet(AnalyzerConfigOptions options, TargetDbms dbms) =>
        HasValue(options, SyntaxKey(dbms)) || HasValue(options, SyntaxMSBuildPropertyKey(dbms));

    /// <summary>
    /// Every family key carrying a value in this file's effective options,
    /// across both surfaces — for value validation, since a typo in the
    /// MSBuild property is exactly as silent as one in the
    /// <c>.editorconfig</c> key.
    /// </summary>
    public static IEnumerable<(string Key, string Value)> SetSyntaxValues(
        AnalyzerConfigOptions options)
    {
        foreach (TargetDbms dbms in AllDbms)
        {
            if (TryGetSetValue(options, SyntaxKey(dbms), out string editorConfigValue))
            {
                yield return (SyntaxKey(dbms), editorConfigValue);
            }

            if (TryGetSetValue(options, SyntaxMSBuildPropertyKey(dbms), out string msBuildValue))
            {
                yield return (SyntaxMSBuildPropertyKey(dbms), msBuildValue);
            }
        }
    }

    private static bool HasValue(AnalyzerConfigOptions options, string key) => TryGetSetValue(
        options,
        key,
        out _);

    /// <summary>
    /// Reads <paramref name="key"/>, treating a blank value as unset: the SDK emits
    /// every declared <c>CompilerVisibleProperty</c> as a key, blank when never set,
    /// so presence alone would make the family govern in every referencing project.
    /// </summary>
    public static bool TryGetSetValue(AnalyzerConfigOptions options, string key, out string value)
    {
        value = options.TryGetValue(key, out string? raw)
            && !string.IsNullOrWhiteSpace(raw) ? raw : string.Empty;
        return value.Length > 0;
    }

    /// <summary>The resolved <c>sqlartisan_syntax_*</c> target set.</summary>
    public static DialectTargetSet ResolveTargets(AnalyzerConfigOptions options)
    {
        var set = new DialectTargetSet();
        foreach (TargetDbms dbms in AllDbms)
        {
            if (!TryResolveSyntaxValue(options, dbms, out string? value))
            {
                continue;
            }

            if (string.Equals(value, NoneValue, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(value, AnyValue, StringComparison.OrdinalIgnoreCase))
            {
                set.Add(dbms, version: null);
            }
            else if (EngineVersion.TryParse(value, out EngineVersion version))
            {
                set.Add(dbms, version);
            }
        }

        return set;
    }

    // An unrecognized .editorconfig value falls through to the MSBuild
    // property rather than resolving to unset.
    private static bool TryResolveSyntaxValue(
        AnalyzerConfigOptions options,
        TargetDbms dbms,
        out string? value)
    {
        if (options.TryGetValue(SyntaxKey(dbms), out string? editorConfigValue)
            && IsRecognizedSyntaxValue(editorConfigValue))
        {
            value = editorConfigValue;
            return true;
        }

        if (options.TryGetValue(SyntaxMSBuildPropertyKey(dbms), out string? msBuildValue)
            && IsRecognizedSyntaxValue(msBuildValue))
        {
            value = msBuildValue;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Enumerates every <c>sqlartisan_syntax_*</c>-prefixed key <paramref name="options"/>
    /// carries, for key-name typo detection (SQLA0001). <see cref="AnalyzerConfigOptions.Keys"/>'s
    /// default implementation throws <see cref="NotImplementedException"/> on a host that
    /// doesn't override it, so a failure here degrades to "skip key-name validation"
    /// rather than take the whole analyzer down.
    /// </summary>
    public static bool TryEnumerateSyntaxKeys(
        AnalyzerConfigOptions options,
        out List<string> keys) =>
        TryEnumeratePrefixedKeys(options, SyntaxKeyPrefix, out keys);

    /// <summary>
    /// Every <c>sqlartisan_construct_*</c>-prefixed key <paramref name="options"/>
    /// carries, for override-value validation across the whole honored surface —
    /// <see cref="ResolveOverride"/> reads any (member, arity) key, not just the
    /// matrix-derived ones, so validating only the latter left honored keys'
    /// typos silent. Degrades like <see cref="TryEnumerateSyntaxKeys"/>.
    /// </summary>
    public static bool TryEnumerateConstructKeys(
        AnalyzerConfigOptions options,
        out List<string> keys) =>
        TryEnumeratePrefixedKeys(options, ConstructKeyNaming.Prefix, out keys);

    private static bool TryEnumeratePrefixedKeys(
        AnalyzerConfigOptions options,
        string prefix,
        out List<string> keys)
    {
        keys = [];
        try
        {
            foreach (string key in options.Keys)
            {
                if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(key);
                }
            }

            return true;
        }
        catch (NotImplementedException)
        {
            return false;
        }
    }

    // The legacy single-DBMS pair, removed in favor of the family (#654). Still read, solely
    // so a project left on it is told why the analyzer stopped checking (SQLA0001): an
    // unconfigured analyzer is silent, so dropping the keys unread would look like success.
    public const string RemovedTargetDbmsKey = "sqlartisan_target_dbms";
    public const string RemovedTargetVersionKey = "sqlartisan_target_version";

    public const string RemovedTargetDbmsMSBuildPropertyKey =
        "build_property.SqlArtisanTargetDbms";

    public const string RemovedTargetVersionMSBuildPropertyKey =
        "build_property.SqlArtisanTargetVersion";

    public static readonly string[] RemovedKeys =
    [
        RemovedTargetDbmsKey, RemovedTargetVersionKey, RemovedTargetDbmsMSBuildPropertyKey,
        RemovedTargetVersionMSBuildPropertyKey,
    ];

    public static IEnumerable<string> DbmsNames => SyntaxDbmsNames.Values;

    /// <summary>
    /// The family setting that replaces a removed key, spelled for the surface the key was
    /// set on. The DBMS and version are read across both surfaces, <c>.editorconfig</c>
    /// first, so the replacement carries the old version over instead of shedding the
    /// dialect's SQLA0101 coverage with an <c>any</c>.
    /// </summary>
    public static string RemovedKeyReplacement(AnalyzerConfigOptions options, string removedKey)
    {
        TargetDbms? dbms = ReadRemovedDbms(options, RemovedTargetDbmsKey)
            ?? ReadRemovedDbms(options, RemovedTargetDbmsMSBuildPropertyKey);
        EngineVersion? version = ReadRemovedVersion(options, RemovedTargetVersionKey)
            ?? ReadRemovedVersion(options, RemovedTargetVersionMSBuildPropertyKey);
        string value = version?.ToString() ?? (dbms is null ? "<version-or-any>" : AnyValue);

        if (!removedKey.StartsWith("build_property.", StringComparison.Ordinal))
        {
            string key = dbms is { } d ? SyntaxKey(d) : SyntaxKeyPrefix + "<dbms>";
            return $"{key} = {value}";
        }

        string property = "SqlArtisanSyntax" + (dbms?.ToString() ?? "<Dbms>");
        return $"<{property}>{value}</{property}>";
    }

    private static TargetDbms? ReadRemovedDbms(AnalyzerConfigOptions options, string key)
    {
        if (!TryGetSetValue(options, key, out string value))
        {
            return null;
        }

        foreach (KeyValuePair<TargetDbms, string> entry in SyntaxDbmsNames)
        {
            if (string.Equals(value, entry.Value, StringComparison.OrdinalIgnoreCase))
            {
                return entry.Key;
            }
        }

        return null;
    }

    private static EngineVersion? ReadRemovedVersion(AnalyzerConfigOptions options, string key) =>
        TryGetSetValue(options, key, out string value)
            && EngineVersion.TryParse(value, out EngineVersion version)
            ? version
            : null;

    /// <summary>
    /// A construct override's raw value, parsed to true (<c>supported</c>),
    /// false (<c>unsupported</c>), or <see langword="null"/> (unset or an
    /// unrecognized value — the latter is separately flagged as SQLA0001).
    /// </summary>
    public static bool? ResolveOverride(AnalyzerConfigOptions options, string overrideKey)
    {
        if (!options.TryGetValue(overrideKey, out string? value))
        {
            return null;
        }

        if (string.Equals(value, "supported", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(value, "unsupported", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return null;
    }

    public static bool IsRecognizedOverrideValue(string value) =>
        string.Equals(value, "supported", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "unsupported", StringComparison.OrdinalIgnoreCase);
}
