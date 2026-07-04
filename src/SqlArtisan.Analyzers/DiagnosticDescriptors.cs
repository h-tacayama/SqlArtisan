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

    public static readonly DiagnosticDescriptor UnsupportedDialectConstruct = new(
        id: "SQLA0001",
        title: "SQL construct not supported on the target dialect",
        messageFormat: "'{0}' is not supported on {1}. Set '{2} = supported' in .editorconfig if your engine version supports it.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkUri);

    public static readonly DiagnosticDescriptor InvalidConfiguration = new(
        id: "SQLA0002",
        title: "Invalid SqlArtisan analyzer configuration",
        messageFormat: "Invalid value '{1}' for '{0}' (expected one of: {2})",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinkUri,
        customTags: WellKnownDiagnosticTags.CompilationEnd);
}
