using Microsoft.CodeAnalysis;

namespace SqlArtisan.Analyzers;

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
