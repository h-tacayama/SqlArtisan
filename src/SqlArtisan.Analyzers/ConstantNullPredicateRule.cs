using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Reports SQLA0007 for <c>IS NULL</c> / <c>IS NOT NULL</c> on a column the
/// schema declares NOT NULL — a predicate whose answer is fixed before the
/// query runs (#266).
/// </summary>
internal static class ConstantNullPredicateRule
{
    // The null-supplying join steps: past any of these, a NOT NULL column can
    // legitimately be NULL, and IS NULL on one is the idiomatic anti-join.
    private static readonly HashSet<string> OuterJoinSteps =
    [
        "LeftJoin", "LeftJoinLateral", "RightJoin", "FullJoin", "OuterApply"
    ];

    public static void Check(OperationAnalysisContext context, IPropertyReferenceOperation predicate)
    {
        // Nullable is the only fact that decides this, and only when it is known
        // to be false: a nullable column makes both predicates meaningful.
        if (SchemaMetadata.Fact(predicate.Instance, SchemaMetadata.NullableArgument) is not false
            || IsInOuterJoinedStatement(predicate))
        {
            return;
        }

        bool isNull = predicate.Property.Name == "IsNull";

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ConstantNullPredicate,
            predicate.Syntax.GetLocation(),
            ((IPropertyReferenceOperation)predicate.Instance!).Property.Name,
            predicate.Property.Name,
            isNull ? "false" : "true"));
    }

    // Whether-the-join-null-supplies-THIS-table is a per-side question this rule
    // does not attempt: any outer join in the statement silences it, which fails
    // toward a missed warning, never a wrong one (ADR 0003).
    private static bool IsInOuterJoinedStatement(IPropertyReferenceOperation predicate)
    {
        IOperation top = predicate;
        while (top.Parent is { } parent and not IBlockOperation)
        {
            top = parent;
        }

        Stack<IOperation> pending = new();
        pending.Push(top);

        while (pending.Count > 0)
        {
            IOperation current = pending.Pop();

            if (current is IInvocationOperation invocation
                && OuterJoinSteps.Contains(invocation.TargetMethod.Name)
                && DialectUsageAnalyzer.IsFromSqlArtisan(invocation.TargetMethod.ContainingAssembly))
            {
                return true;
            }

            foreach (IOperation child in current.ChildOperations)
            {
                pending.Push(child);
            }
        }

        return false;
    }
}
