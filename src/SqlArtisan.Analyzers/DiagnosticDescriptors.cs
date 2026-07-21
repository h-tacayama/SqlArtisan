using Microsoft.CodeAnalysis;

namespace SqlArtisan.Analyzers;

// Message length standard (benchmarked against the BCL analyzers): a fact-only
// message is one short sentence (IDE0305/CS0103 are 40-60 chars); fact plus
// remediation is two sentences, capped around CS8618's ~155 chars. Lead with the
// fact, include only remediation the user cannot guess (the derived override
// key), and push everything else to docs/analyzer.md via the help link.
internal static class DiagnosticDescriptors
{
    private const string Category = "SqlArtisan.Dialect";
    private const string HelpLinkUri = "https://github.com/h-tacayama/SqlArtisan/blob/main/docs/analyzer.md";

    // {2} carries its own "one of: "/"a numeric ..." lead-in per call site (the
    // target-dbms/override-value/target-version keys don't all read naturally
    // under one fixed lead-in phrase).
    public static readonly DiagnosticDescriptor InvalidConfiguration = new(
        id: "SQLA0001",
        title: "Invalid SqlArtisan analyzer configuration",
        messageFormat: "Invalid value '{1}' for '{0}' (expected {2})",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkUri,
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    public static readonly DiagnosticDescriptor UnsupportedDialectConstruct = new(
        id: "SQLA0002",
        title: "SQL construct not supported on the target dialect",
        messageFormat: "'{0}' is not supported on {1}. Set '{2} = supported' in .editorconfig if your engine version supports it.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkUri);

    // No override-key hint: the construct itself is supported on the target, so
    // sqlartisan_construct_* keys do not apply — suppression is per-ID only.
    public static readonly DiagnosticDescriptor ContextRestrictedConstruct = new(
        id: "SQLA0003",
        title: "SQL construct not supported in this position on the target dialect",
        messageFormat: "'{0}' is not supported {1} on {2}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkUri);

    public static readonly DiagnosticDescriptor IdentifierTooLong = new(
        id: "SQLA0004",
        title: "SQL identifier exceeds the dialect's length limit",
        messageFormat: "Identifier '{0}' exceeds {1}'s identifier limit of {2} {3}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkUri);

    // Mirrors the Build()-time guard's message (parity-tested, modulo the
    // trailing period RS1032 forbids on a single-sentence diagnostic): the
    // diagnostic is the same finding surfaced earlier, and suppressing it does
    // not disable the throw.
    public static readonly DiagnosticDescriptor CorrelatedDmlTargetNotAliased = new(
        id: "SQLA0005",
        title: "Correlated UPDATE or DELETE target is not aliased",
        messageFormat: "The target of a correlated UPDATE or DELETE must be aliased",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkUri);

    // Distinct from SQLA0002 (#263): the dialect itself supports the construct, but
    // not at the caller's declared sqlartisan_target_version — a version shortfall,
    // not a dialect mismatch, so the remediation differs (raise the version, or
    // override if the caller has verified their actual engine already supports it).
    public static readonly DiagnosticDescriptor VersionBoundConstruct = new(
        id: "SQLA0006",
        title: "SQL construct requires a newer engine version than the declared target",
        messageFormat: "'{0}' requires {1} {2}+ but the declared target version is {3}. Set '{4} = supported' in .editorconfig if your engine supports it.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkUri);
}
