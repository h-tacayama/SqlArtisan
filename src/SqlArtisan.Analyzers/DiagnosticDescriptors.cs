using Microsoft.CodeAnalysis;

namespace SqlArtisan.Analyzers;

// Message length standard (benchmarked against the BCL analyzers): a fact-only
// message is one short sentence (IDE0305/CS0103 are 40-60 chars); fact plus
// remediation is two sentences, capped around CS8618's ~155 chars. Lead with the
// fact, include only remediation the user cannot guess (the derived override
// key), and push everything else to docs/analyzer.md via the help link.
internal static class DiagnosticDescriptors
{
    // Ids are banded by category so a family grows without renumbering (the flat
    // scheme ran out at the sixth dialect rule, #326/#349) and so a bulk-severity
    // setting reaches one family only (#266). DiagnosticOrderingTests gates it.
    private const string ConfigurationCategory = "SqlArtisan.Configuration";
    private const string DialectCategory = "SqlArtisan.Dialect";
    private const string SchemaCategory = "SqlArtisan.Schema";

    // Holds one rule today. The band exists because mirroring a Build()-time
    // guard is a shape the library can repeat, not because a queue is waiting.
    private const string ValidityCategory = "SqlArtisan.Validity";

    private const string HelpLinkUri = "https://github.com/h-tacayama/SqlArtisan/blob/main/docs/analyzer.md";

    // {2} carries its own "one of: "/"a numeric ..." lead-in per call site (the
    // target-dbms/override-value/target-version keys don't all read naturally
    // under one fixed lead-in phrase).
    public static readonly DiagnosticDescriptor InvalidConfiguration = new(
        id: "SQLA0001",
        title: "Invalid SqlArtisan analyzer configuration",
        messageFormat: "Invalid value '{1}' for '{0}' (expected {2})",
        category: ConfigurationCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkUri,
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    public static readonly DiagnosticDescriptor UnsupportedDialectConstruct = new(
        id: "SQLA0100",
        title: "SQL construct not supported on the target dialect",
        messageFormat: "'{0}' is not supported on {1}. Set '{2} = supported' in .editorconfig if your engine version supports it.",
        category: DialectCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkUri);

    // Distinct from SQLA0100 (#263): the dialect itself supports the construct, but
    // not at the caller's declared sqlartisan_target_version — a version shortfall,
    // not a dialect mismatch, so the remediation differs (raise the version, or
    // override if the caller has verified their actual engine already supports it).
    public static readonly DiagnosticDescriptor VersionBoundConstruct = new(
        id: "SQLA0101",
        title: "SQL construct requires a newer engine version than the declared target",
        messageFormat: "'{0}' requires {1} {2}+ but the declared target version is {3}. Set '{4} = supported' in .editorconfig if your engine supports it.",
        category: DialectCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkUri);

    // No override-key hint: the construct itself is supported on the target, so
    // sqlartisan_construct_* keys do not apply — suppression is per-ID only.
    public static readonly DiagnosticDescriptor ContextRestrictedConstruct = new(
        id: "SQLA0102",
        title: "SQL construct not supported in this position on the target dialect",
        messageFormat: "'{0}' is not supported {1} on {2}",
        category: DialectCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkUri);

    public static readonly DiagnosticDescriptor IdentifierTooLong = new(
        id: "SQLA0103",
        title: "SQL identifier exceeds the dialect's length limit",
        messageFormat: "Identifier '{0}' exceeds {1}'s identifier limit of {2} {3}",
        category: DialectCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkUri);

    // Schema-derived, so it names no dialect: the column's own declaration decides
    // the answer on every engine. Silent unless the table class carries the fact.
    public static readonly DiagnosticDescriptor ConstantNullPredicate = new(
        id: "SQLA0200",
        title: "SQL predicate is constant for a NOT NULL column",
        messageFormat: "'{0}' is NOT NULL, so '{1}' is always {2}",
        category: SchemaCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkUri);

    // States the consequence rather than "is nullable": the failure is total and
    // silent — no rows at all — which is what makes it worth a warning.
    public static readonly DiagnosticDescriptor NotInNullableSubquery = new(
        id: "SQLA0201",
        title: "NOT IN over a nullable subquery column returns no rows when it yields NULL",
        messageFormat: "'{0}' is nullable, so this NOT IN matches no rows at all when the subquery yields a NULL",
        category: SchemaCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkUri);

    // Names no dialect, though MySQL outside strict mode substitutes an implicit
    // default instead of failing — a docs caveat, not a reason to stay silent
    // where the same statement is rejected everywhere else.
    public static readonly DiagnosticDescriptor InsertMissingRequiredColumn = new(
        id: "SQLA0202",
        title: "INSERT omits a column that is NOT NULL with no default",
        messageFormat: "'{0}' is NOT NULL with no default and is missing from this INSERT's column list",
        category: SchemaCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkUri);

    // Counting non-NULL values is a real intent, so this is advice on code that may
    // well be right — off until named explicitly (a category-wide severity does not
    // reach a disabled rule), and Info so it stays out of build output.
    public static readonly DiagnosticDescriptor CountNullableColumn = new(
        id: "SQLA0203",
        title: "COUNT of a nullable column counts values, not rows",
        messageFormat: "'{0}' is nullable, so this COUNT skips its NULL rows. Use Count(Asterisk) to count rows.",
        category: SchemaCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        helpLinkUri: HelpLinkUri);

    // One ID for every shape because the remediation is one: leave the column bare
    // on the filtered side. States the form, never the cost — whether the planner
    // would have taken the index is Tier 3.
    public static readonly DiagnosticDescriptor UnusableIndexPredicate = new(
        id: "SQLA0204",
        title: "Filter shapes an indexed column so no index on it can be used",
        messageFormat: "'{0}' leads an index, but this filter has it {1}, so no index on it can be used",
        category: SchemaCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkUri);

    // A defect diagnostic rather than a performance opinion: MySQL compares a string
    // to a number as floating point, so rows the author did not mean can match. That
    // the index also goes unused is the lesser half.
    public static readonly DiagnosticDescriptor TypeCategoryMismatch = new(
        id: "SQLA0205",
        title: "Column compared to a value of another type category",
        messageFormat: "'{0}' is {1}, but this compares it to {2}. Cast one side to say which you mean.",
        category: SchemaCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkUri);

    // Mirrors the Build()-time guard's message (parity-tested, modulo the
    // trailing period RS1032 forbids on a single-sentence diagnostic): the
    // diagnostic is the same finding surfaced earlier, and suppressing it does
    // not disable the throw.
    public static readonly DiagnosticDescriptor CorrelatedDmlTargetNotAliased = new(
        id: "SQLA0300",
        title: "Correlated UPDATE or DELETE target is not aliased",
        messageFormat: "The target of a correlated UPDATE or DELETE must be aliased",
        category: ValidityCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkUri);
}
