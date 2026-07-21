using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Fluent-chain steps shared by the rules that walk a SqlArtisan builder chain
/// (the #264 context rules, the #256 correlated-DML rule).
/// </summary>
internal static class FluentChain
{
    public static IInvocationOperation? Parent(IOperation current)
    {
        IOperation unwrapped = SkipConversion(current);
        return unwrapped.Parent is IInvocationOperation invocation
            && invocation.Instance == unwrapped
            && DialectUsageAnalyzer.IsFromSqlArtisan(invocation.TargetMethod.ContainingAssembly)
                ? invocation
                : null;
    }

    // An operation used as a typed argument or receiver often sits behind an
    // implicit conversion.
    public static IOperation SkipConversion(IOperation operation) =>
        operation.Parent is IConversionOperation conversion ? conversion : operation;
}
