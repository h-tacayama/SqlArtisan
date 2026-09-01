using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Fluent-chain steps shared by the rules that walk a SqlArtisan builder chain.
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

                // A static factory (e.g. Not) only wraps the node — the chain it feeds
                // is further out; a step with a receiver is the chain, head unresolved.
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
    /// Any outer join counts — which side it null-supplies is not asked. Sound
    /// only where <see cref="HasVisibleStatementHead"/> holds: a chain reaching
    /// beyond the statement can join outside what this walks.
    /// </remarks>
    public static bool HasOuterJoin(IOperation node)
    {
        // The outer statement's own spine: every invocation ancestor of the
        // reported node plus each ancestor's receiver chain down to the head.
        HashSet<IOperation> spine = [];
        IOperation top = node;
        CollectSpine(node, spine);
        while (top.Parent is { } parent and not IBlockOperation)
        {
            top = parent;
            CollectSpine(top, spine);
        }

        Stack<IOperation> pending = new();
        pending.Push(top);

        while (pending.Count > 0)
        {
            IOperation current = pending.Pop();

            if (current is IInvocationOperation invocation
                && DialectUsageAnalyzer.IsFromSqlArtisan(invocation.TargetMethod.ContainingAssembly))
            {
                // A chain rooted at its own statement head is a nested subquery
                // — its joins say nothing about the outer statement's shape.
                if (!spine.Contains(invocation) && IsStatementHead(invocation))
                {
                    continue;
                }

                if (OuterJoinSteps.Contains(invocation.TargetMethod.Name))
                {
                    return true;
                }
            }

            foreach (IOperation child in current.ChildOperations)
            {
                pending.Push(child);
            }
        }

        return false;
    }

    private static void CollectSpine(IOperation operation, HashSet<IOperation> spine)
    {
        IOperation? link = operation;
        while (link is IInvocationOperation invocation)
        {
            spine.Add(invocation);
            link = invocation.Instance is null ? null : Unwrap(invocation.Instance);
        }
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

    /// <summary>
    /// Whether <paramref name="type"/> derives from <c>SqlExpression</c> — the
    /// receiver type of the predicate-building steps that sit inside a
    /// predicate rather than consuming one.
    /// </summary>
    public static bool IsExpression(ITypeSymbol? type)
    {
        for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (current.Name == "SqlExpression"
                && DialectUsageAnalyzer.IsFromSqlArtisan(current.ContainingAssembly))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether <paramref name="type"/> derives from <c>TableReference</c>, so a
    /// property declared on it is a genuine column rather than an arbitrary
    /// DbColumn-typed value.
    /// </summary>
    public static bool IsTableReference(ITypeSymbol? type)
    {
        for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (current.Name == "TableReference"
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
