using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Reads <c>sqlartisan_syntax_*</c> (and the legacy <c>sqlartisan_target_dbms</c>
/// / <c>sqlartisan_construct_*</c>) from <see cref="AnalyzerConfigOptions"/> (the
/// <c>.editorconfig</c> / MSBuild property surface Roslyn exposes to analyzers).
/// Values are looked up per-syntax-tree, so a <c>.editorconfig</c> section
/// scoped to a directory naturally gives that directory its own target set —
/// no extra plumbing needed.
/// </summary>
internal static class AnalyzerConfigResolver
{
    public static readonly TargetDbms[] AllDbms =
    [
        TargetDbms.MySql, TargetDbms.Oracle, TargetDbms.PostgreSql, TargetDbms.Sqlite, TargetDbms.SqlServer,
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
    /// the <c>CompilerVisibleProperty</c> entries declared alongside the legacy
    /// pair's (src/SqlArtisan.Analyzers/build/SqlArtisan.props).
    /// </summary>
    public static string SyntaxMSBuildPropertyKey(TargetDbms dbms) => $"build_property.SqlArtisanSyntax{dbms}";

    public static bool IsRecognizedSyntaxValue(string value) =>
        string.Equals(value, AnyValue, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, NoneValue, StringComparison.OrdinalIgnoreCase)
        || EngineVersion.TryParse(value, out _);

    /// <summary>Whether <paramref name="key"/> is one of the five <c>sqlartisan_syntax_&lt;dbms&gt;</c> keys, any casing.</summary>
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
    /// Whether any <c>sqlartisan_syntax_*</c> key carries a value anywhere in
    /// this file's effective options — <c>.editorconfig</c> or the
    /// MSBuild-property fallback, a *recognized* value or not. Any value makes
    /// the family govern the whole resolution (#432's family-wins-outright
    /// precedence); an invalid one still counts, so a mistyped family *value*
    /// never silently lets the legacy pair take over. (A mistyped key *name*
    /// does — no exact key matches — which is what the separate
    /// <see cref="TryEnumerateSyntaxKeys"/> validation exists to flag.)
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

    /// <summary>Whether the family names <paramref name="dbms"/> on either surface, with any non-blank value.</summary>
    public static bool IsFamilyKeySet(AnalyzerConfigOptions options, TargetDbms dbms) =>
        HasValue(options, SyntaxKey(dbms)) || HasValue(options, SyntaxMSBuildPropertyKey(dbms));

    /// <summary>
    /// Every family key carrying a value in this file's effective options,
    /// across both surfaces — for value validation, since a typo in the
    /// MSBuild property is exactly as silent as one in the
    /// <c>.editorconfig</c> key.
    /// </summary>
    public static IEnumerable<(string Key, string Value)> SetSyntaxValues(AnalyzerConfigOptions options)
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

    private static bool HasValue(AnalyzerConfigOptions options, string key) => TryGetSetValue(options, key, out _);

    /// <summary>
    /// Reads <paramref name="key"/>, treating a blank value as unset: the
    /// shipped props declares a <c>CompilerVisibleProperty</c> per DBMS, and
    /// the SDK emits every declared property as a key — with an empty value
    /// when the consumer never set it — so testing key presence alone would
    /// make the family govern in every project referencing the package.
    /// </summary>
    private static bool TryGetSetValue(AnalyzerConfigOptions options, string key, out string value)
    {
        value = options.TryGetValue(key, out string? raw) && !string.IsNullOrWhiteSpace(raw) ? raw : string.Empty;
        return value.Length > 0;
    }

    /// <summary>
    /// The resolved target set: the <c>sqlartisan_syntax_*</c> family if any key
    /// in it is present (govern outright — never merged with the legacy pair),
    /// otherwise the legacy pair desugared to a single-DBMS set.
    /// </summary>
    public static DialectTargetSet ResolveTargets(AnalyzerConfigOptions options) =>
        IsFamilyPresent(options) ? ResolveFamilyTargets(options) : ResolveLegacyTargets(options);

    private static DialectTargetSet ResolveFamilyTargets(AnalyzerConfigOptions options)
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

    // .editorconfig wins when its value is recognized; an unrecognized
    // .editorconfig value falls through to the MSBuild property rather than
    // resolving to unset outright — the same precedent ResolveTargetVersion
    // already sets for the legacy pair.
    private static bool TryResolveSyntaxValue(AnalyzerConfigOptions options, TargetDbms dbms, out string? value)
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

    private static DialectTargetSet ResolveLegacyTargets(AnalyzerConfigOptions options)
    {
        if (ResolveTarget(options) is not { } target)
        {
            return DialectTargetSet.Empty;
        }

        var set = new DialectTargetSet();
        set.Add(target, ResolveTargetVersion(options));
        return set;
    }

    /// <summary>
    /// Enumerates every <c>sqlartisan_syntax_*</c>-prefixed key <paramref name="options"/>
    /// carries, for key-name typo detection (SQLA0001). <see cref="AnalyzerConfigOptions.Keys"/>'s
    /// default implementation throws <see cref="NotImplementedException"/> on a host that
    /// doesn't override it, so a failure here degrades to "skip key-name validation"
    /// rather than take the whole analyzer down.
    /// </summary>
    public static bool TryEnumerateSyntaxKeys(AnalyzerConfigOptions options, out List<string> keys)
    {
        keys = [];
        try
        {
            foreach (string key in options.Keys)
            {
                if (key.StartsWith(SyntaxKeyPrefix, StringComparison.OrdinalIgnoreCase))
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

    public const string TargetDbmsKey = "sqlartisan_target_dbms";

    /// <summary>
    /// The MSBuild-property fallback for <see cref="TargetDbmsKey"/>, populated
    /// via the <c>CompilerVisibleProperty</c> declared in the shipped
    /// buildTransitive props (src/SqlArtisan.Analyzers/build/SqlArtisan.props). Consumers
    /// who prefer setting <c>&lt;SqlArtisanTargetDbms&gt;</c> in a .csproj /
    /// Directory.Build.props over an .editorconfig section use this key
    /// instead; .editorconfig wins when both are set.
    /// </summary>
    public const string TargetDbmsMSBuildPropertyKey = "build_property.SqlArtisanTargetDbms";

    private static readonly Dictionary<string, TargetDbms> TargetNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["mysql"] = TargetDbms.MySql,
        ["oracle"] = TargetDbms.Oracle,
        ["postgresql"] = TargetDbms.PostgreSql,
        ["sqlite"] = TargetDbms.Sqlite,
        ["sqlserver"] = TargetDbms.SqlServer,
    };

    public static IEnumerable<string> ValidTargetNames => TargetNames.Keys;

    /// <summary>
    /// The configured target for this syntax tree, or <see langword="null"/> if
    /// unset or unrecognized (the analyzer stays silent in either case — an
    /// unrecognized value is separately flagged as SQLA0001).
    /// </summary>
    public static TargetDbms? ResolveTarget(AnalyzerConfigOptions options)
    {
        if (options.TryGetValue(TargetDbmsKey, out string? editorConfigValue)
            && TargetNames.TryGetValue(editorConfigValue, out TargetDbms fromEditorConfig))
        {
            return fromEditorConfig;
        }

        if (options.TryGetValue(TargetDbmsMSBuildPropertyKey, out string? msBuildValue)
            && TargetNames.TryGetValue(msBuildValue, out TargetDbms fromMsBuildProperty))
        {
            return fromMsBuildProperty;
        }

        return null;
    }

    public static bool IsRecognizedTargetValue(string value) => TargetNames.ContainsKey(value);

    /// <summary>The #262 reserved key: the engine version bounds are evaluated against.</summary>
    public const string TargetVersionKey = "sqlartisan_target_version";

    /// <summary>The MSBuild-property fallback for <see cref="TargetVersionKey"/>, same shape as <see cref="TargetDbmsMSBuildPropertyKey"/>.</summary>
    public const string TargetVersionMSBuildPropertyKey = "build_property.SqlArtisanTargetVersion";

    /// <summary>
    /// The declared target version for this syntax tree, or <see langword="null"/>
    /// if unset or unparseable (an unparseable value is separately flagged as
    /// SQLA0001; either way version bounds simply do not apply).
    /// </summary>
    public static EngineVersion? ResolveTargetVersion(AnalyzerConfigOptions options)
    {
        if (options.TryGetValue(TargetVersionKey, out string? editorConfigValue)
            && EngineVersion.TryParse(editorConfigValue, out EngineVersion fromEditorConfig))
        {
            return fromEditorConfig;
        }

        if (options.TryGetValue(TargetVersionMSBuildPropertyKey, out string? msBuildValue)
            && EngineVersion.TryParse(msBuildValue, out EngineVersion fromMsBuildProperty))
        {
            return fromMsBuildProperty;
        }

        return null;
    }

    public static bool IsRecognizedVersionValue(string value) => EngineVersion.TryParse(value, out _);

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
