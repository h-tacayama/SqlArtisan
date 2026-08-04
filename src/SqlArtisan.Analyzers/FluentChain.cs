using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Fluent-chain steps shared by the rules that walk a SqlArtisan builder chain
/// (the #264 context rules, the #256 correlated-DML rule, the #266 schema
/// rules).
/// </summary>
internal static class FluentChain
{
    // Internal for the parity gate: a join step the core adds must be classified
    // here deliberately, not discovered through a user's false positive.
    internal static readonly HashSet<string> OuterJoinSteps =
    [
        "LeftJoin", "LeftJoinLateral", "RightJoin", "FullJoin", "OuterApply",
        "NaturalLeftJoin", "NaturalRightJoin", "NaturalFullJoin"
    ];

    private static readonly HashSet<string> StatementHeads =
    [
        "Select", "Update", "DeleteFrom", "MergeInto", "With", "WithRecursive"
    ];

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

    /// <summary>
    /// Whether <paramref name="node"/> belongs to a chain whose statement head is
    /// visible in the same statement — the precondition for reading the query's
    /// shape off that statement.
    /// </summary>
    /// <remarks>
    /// A chain assembled across statements, in a helper method, or in a field
    /// hides the joins that decide a column's nullability, so a rule that judges
    /// one must first establish it can see the whole query (#266).
    /// </remarks>
    public static bool HasVisibleStatementHead(IOperation node)
    {
        IOperation current = node;

        while (current.Parent is { } parent and not IBlockOperation)
        {
            if (parent is IInvocationOperation step
                && DialectUsageAnalyzer.IsFromSqlArtisan(step.TargetMethod.ContainingAssembly))
            {
                if (IsStatementHead(step))
                {
                    return true;
                }

                // A static factory (Not, ConditionIf, Coalesce) only wraps the
                // node in argument position; the chain it feeds is further out. A
                // step with a receiver is the chain, and its head did not resolve.
                if (step.Instance is not null)
                {
                    return false;
                }
            }

            current = parent;
        }

        return false;
    }

    /// <summary>
    /// Whether the statement containing <paramref name="node"/> null-supplies any
    /// row — the case that makes a NOT NULL column legitimately NULL.
    /// </summary>
    /// <remarks>
    /// Which side a join null-supplies is a per-side question this does not
    /// answer: any outer join counts. Sound only where
    /// <see cref="HasVisibleStatementHead"/> holds, since a chain that reaches
    /// beyond the statement can join outside what this walks.
    /// </remarks>
    public static bool HasOuterJoin(IOperation node)
    {
        IOperation top = node;
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

    /// <summary>
    /// Whether <paramref name="type"/> is a SqlArtisan condition — the type a
    /// predicate has wherever it is written, held, or returned from.
    /// </summary>
    public static bool IsCondition(ITypeSymbol? type)
    {
        for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (current.Name == "SqlCondition"
                && DialectUsageAnalyzer.IsFromSqlArtisan(current.ContainingAssembly))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsStatementHead(IInvocationOperation step)
    {
        IOperation current = step;

        while (current is IInvocationOperation invocation)
        {
            if (invocation.Instance is null)
            {
                return StatementHeads.Contains(invocation.TargetMethod.Name)
                    && DialectUsageAnalyzer.IsFromSqlArtisan(invocation.TargetMethod.ContainingAssembly);
            }

            current = Unwrap(invocation.Instance);
        }

        return false;
    }

    private static IOperation Unwrap(IOperation operation) =>
        operation is IConversionOperation conversion ? conversion.Operand : operation;
}
