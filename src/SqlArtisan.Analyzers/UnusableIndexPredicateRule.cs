using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Reports SQLA0011 when a filter shapes an indexed column so no index on it can
/// be used — the column wrapped in a function, or matched with a leading-wildcard
/// <c>LIKE</c> (#266).
/// </summary>
/// <remarks>
/// The predicate's form, never its cost: whether the planner would have chosen
/// the index is Tier 3. A column the schema does not record as leading an index
/// claims nothing, so the rule is silent there.
/// </remarks>
internal static class UnusableIndexPredicateRule
{
    // Having is absent deliberately: it filters groups, and an aggregate there is
    // computed after any index has already done its work.
    private static readonly HashSet<string> FilteringSteps = ["Where", "On"];

    public static void CheckFunctionCall(OperationAnalysisContext context, IInvocationOperation call)
    {
        if (IndexedArgument(call) is not { } column || !IsInsideFilter(call))
        {
            return;
        }

        Report(context, call, column.Property.Name, $"wrapped in {call.TargetMethod.Name}");
    }

    public static void CheckLike(OperationAnalysisContext context, IInvocationOperation like)
    {
        if (Unwrap(like.Instance) is not IPropertyReferenceOperation column
            || SchemaMetadata.Fact(column, SchemaMetadata.IndexedArgument) is not true
            || Unwrap(like.Arguments[0].Value) is not
            { ConstantValue: { HasValue: true, Value: string pattern } }
            || !pattern.StartsWith("%", System.StringComparison.Ordinal)
            || !IsInsideFilter(like))
        {
            return;
        }

        Report(context, like, column.Property.Name, "matched with a leading-wildcard pattern");
    }

    private static void Report(
        OperationAnalysisContext context,
        IOperation predicate,
        string columnName,
        string shape)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.UnusableIndexPredicate,
            predicate.Syntax.GetLocation(),
            columnName,
            shape));
    }

    // Only a column the call itself wraps: a function over some other expression
    // that merely mentions the column is not the shape this reports.
    private static IPropertyReferenceOperation? IndexedArgument(IInvocationOperation call)
    {
        foreach (IArgumentOperation argument in call.Arguments)
        {
            if (Unwrap(argument.Value) is IPropertyReferenceOperation column
                && SchemaMetadata.Fact(column, SchemaMetadata.IndexedArgument) is true)
            {
                return column;
            }
        }

        return null;
    }

    // A predicate reaches the engine's planner only from a filtering clause; the
    // same call in a select list or an ORDER BY costs no index. A condition built
    // apart from its clause is left alone rather than guessed at.
    private static bool IsInsideFilter(IOperation node)
    {
        IOperation current = node;

        while (current.Parent is { } parent and not IBlockOperation)
        {
            if (parent is IInvocationOperation step
                && DialectUsageAnalyzer.IsFromSqlArtisan(step.TargetMethod.ContainingAssembly)
                && step.Instance is not null)
            {
                return FilteringSteps.Contains(step.TargetMethod.Name);
            }

            current = parent;
        }

        return false;
    }

    private static IOperation? Unwrap(IOperation? operation) =>
        operation is IConversionOperation conversion ? conversion.Operand : operation;
}
