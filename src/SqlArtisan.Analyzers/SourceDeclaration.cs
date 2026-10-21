using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace SqlArtisan.Analyzers;

/// <summary>
/// The one way a rule reads a symbol's declaration: from a syntax tree of the
/// compilation under analysis, or not at all.
/// </summary>
/// <remarks>
/// A referenced project's source is reachable from its symbols in the IDE, yet
/// GetSemanticModel throws on it (AD0001), and an emitted reference carries none —
/// so a declaration outside the compilation is unknown, and the rule is silent.
/// </remarks>
internal static class SourceDeclaration
{
    public static T? Single<T>(Compilation compilation, ISymbol symbol)
        where T : SyntaxNode =>
        symbol.DeclaringSyntaxReferences is { Length: 1 } references
            && compilation.ContainsSyntaxTree(references[0].SyntaxTree)
            ? references[0].GetSyntax() as T
            : null;

    // Every part of a partial declaration, or null when any part lies outside.
    public static SyntaxNode[]? All(Compilation compilation, ISymbol symbol)
    {
        ImmutableArray<SyntaxReference> references = symbol.DeclaringSyntaxReferences;
        SyntaxNode[] nodes = new SyntaxNode[references.Length];
        for (int i = 0; i < references.Length; i++)
        {
            if (!compilation.ContainsSyntaxTree(references[i].SyntaxTree))
            {
                return null;
            }

            nodes[i] = references[i].GetSyntax();
        }

        return nodes;
    }

    public static SemanticModel? Model(Compilation compilation, SyntaxNode node) =>
        compilation.ContainsSyntaxTree(node.SyntaxTree)
            ? compilation.GetSemanticModel(node.SyntaxTree)
            : null;
}
