using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzers;

/// <summary>
/// The never-both-fire contract SQLA0104's two rules share: a dialect
/// SQLA0100/SQLA0101 already flags is theirs to report, so a finer-grained
/// verdict on the same usage stays silent there (#449).
/// </summary>
/// <remarks>
/// An <c>unsupported</c> override makes SQLA0100 fire for every target, which is
/// what <see cref="For"/>'s null return means; a <c>supported</c> override re-arms
/// the finer check, since it claims the construct runs, not that every value does.
/// </remarks>
internal readonly struct ValueDomainScope
{
    private readonly DialectSupportResolver.MatrixMatch? _match;
    private readonly bool _isAsserted;

    private ValueDomainScope(DialectSupportResolver.MatrixMatch? match, bool isAsserted)
    {
        _match = match;
        _isAsserted = isAsserted;
    }

    /// <summary>
    /// The scope for <paramref name="invocation"/>, or <see langword="null"/>
    /// when an <c>unsupported</c> override has already handed the usage to
    /// SQLA0100 on every target.
    /// </summary>
    public static ValueDomainScope? For(
        OperationAnalysisContext context,
        IInvocationOperation invocation)
    {
        string memberName = invocation.TargetMethod.Name;
        int arity = invocation.TargetMethod.Parameters.Length;

        AnalyzerConfigOptions options =
            context.Options.AnalyzerConfigOptionsProvider.GetOptions(invocation.Syntax.SyntaxTree);
        DialectSupportResolver.OverrideResult? overrideResult =
            DialectSupportResolver.ResolveOverride(options, memberName, arity);

        return overrideResult is { IsSupported: false }
            ? null
            : new ValueDomainScope(
                DialectSupportResolver.MatchMatrixEntry(memberName, arity),
                overrideResult is { IsSupported: true });
    }

    /// <summary>
    /// Whether a value-domain rule may speak for <paramref name="dbms"/> — true
    /// unless the matrix already flags the construct unsupported there at the
    /// declared version.
    /// </summary>
    public bool Covers(TargetDbms dbms, DialectTargetSet targets) =>
        _isAsserted
        || _match is not { } match
        || DialectSupportResolver.Evaluate(match, dbms, targets.VersionFor(dbms)).IsSupported;
}
