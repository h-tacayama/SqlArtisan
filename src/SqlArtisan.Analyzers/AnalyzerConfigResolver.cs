using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Reads <c>sqlartisan_target_dbms</c> and <c>sqlartisan_construct_*</c> from
/// <see cref="AnalyzerConfigOptions"/> (the <c>.editorconfig</c> / MSBuild
/// property surface Roslyn exposes to analyzers). Values are looked up
/// per-syntax-tree, so a <c>.editorconfig</c> section scoped to a directory
/// naturally gives that directory its own target — no extra plumbing needed.
/// </summary>
internal static class AnalyzerConfigResolver
{
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
    /// unrecognized value is separately flagged as SQLA0002).
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

    /// <summary>
    /// A construct override's raw value, parsed to true (<c>supported</c>),
    /// false (<c>unsupported</c>), or <see langword="null"/> (unset or an
    /// unrecognized value — the latter is separately flagged as SQLA0002).
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
