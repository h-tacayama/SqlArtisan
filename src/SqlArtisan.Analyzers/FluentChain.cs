using System.Collections.Generic;
using System.Collections.Immutable;
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

    // A super-aggregate row carries NULL in a NOT NULL grouping column, so these
    // null-supply as an outer join does. Internal for the parity gate.
    internal static readonly HashSet<string> NullSupplyingGroupings =
    [
        "Cube", "GroupingSets", "Rollup", "WithRollup"
    ];

    private static readonly HashSet<string> StatementHeads =
    [
        "Select", "InsertInto", "InsertIgnoreInto", "Update", "DeleteFrom", "MergeInto", "With",
        "WithRecursive"
    ];

    // A set operator opens a new query block, so the branch after it is its own
    // statement for the rules — a predicate there cannot see the other branch's
    // joins. Internal for the parity gate, like OuterJoinSteps.
    internal static readonly HashSet<string> SetOperators =
    [
        "Except", "ExceptAll", "Intersect", "IntersectAll", "Minus", "MinusAll", "Union", "UnionAll"
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
    /// Whether <paramref name="operation"/> is a call into code that is not
    /// SqlArtisan — a helper that may place its argument anywhere, so the chain
    /// above it is not the one the argument ends in.
    /// </summary>
    public static bool IsForeignInvocation(IOperation operation) =>
        operation is IInvocationOperation invocation
        && !DialectUsageAnalyzer.IsFromSqlArtisan(invocation.TargetMethod.ContainingAssembly);

    /// <summary>
    /// Whether <paramref name="node"/> belongs to a chain whose statement head is
    /// visible in the same statement, as is every chain that query is nested in —
    /// the precondition for reading the query's shape off that statement.
    /// </summary>
    /// <remarks>
    /// A chain assembled across statements, in a helper method, or in a field
    /// hides the joins that decide a column's nullability (#266), and a correlated
    /// subquery reads the joins of every chain it sits in.
    /// </remarks>
    public static bool HasVisibleStatementHead(IOperation node)
    {
        IOperation current = node;
        bool headed = false;

        while (current.Parent is { } parent and not IBlockOperation)
        {
            // Past a complete statement, foreign code only executes or holds it.
            if (IsForeignInvocation(parent))
            {
                return headed;
            }

            if (parent is IInvocationOperation step
                && DialectUsageAnalyzer.IsFromSqlArtisan(step.TargetMethod.ContainingAssembly))
            {
                if (IsStatementHead(step))
                {
                    headed = true;
                }
                else if (step.Instance is not null)
                {
                    // A static factory (e.g. Not) only wraps the node — the chain it
                    // feeds is further out; a step with a receiver is the chain.
                    return false;
                }
            }

            current = parent;
        }

        return headed;
    }

    /// <summary>
    /// Whether <paramref name="table"/> is a table some visible chain around
    /// <paramref name="node"/> reads — its own query block's or an enclosing one's.
    /// </summary>
    /// <remarks>
    /// A column of any other table is correlated to a query this cannot see, whose
    /// joins decide its nullability. A table not held in a local, parameter, field
    /// or property is unknown, and so not read.
    /// </remarks>
    public static bool IsReadByVisibleChain(IOperation node, IOperation table)
    {
        if (ReferencedSymbol(table) is not { } symbol)
        {
            return false;
        }

        for (IOperation? current = node.Parent;
            current is not null and not IBlockOperation;
            current = current.Parent)
        {
            if (current is not IInvocationOperation step
                || !DialectUsageAnalyzer.IsFromSqlArtisan(step.TargetMethod.ContainingAssembly))
            {
                continue;
            }

            for (IOperation? link = step;
                link is IInvocationOperation invocation;
                link = invocation.Instance is null ? null : Unwrap(invocation.Instance))
            {
                foreach (IArgumentOperation argument in invocation.Arguments)
                {
                    // A params table list arrives as an implicit array.
                    IEnumerable<IOperation> values =
                        argument.Value is IArrayCreationOperation { Initializer: { } list }
                            ? list.ElementValues
                            : ImmutableArray.Create(argument.Value);
                    foreach (IOperation value in values)
                    {
                        if (IsTableReference(Unwrap(value).Type)
                            && SymbolEqualityComparer.Default.Equals(
                                ReferencedSymbol(value), symbol))
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    private static ISymbol? ReferencedSymbol(IOperation operation) => Unwrap(operation) switch
    {
        ILocalReferenceOperation local => local.Local,
        IParameterReferenceOperation parameter => parameter.Parameter,
        IFieldReferenceOperation field => field.Field,
        IPropertyReferenceOperation property => property.Property,
        _ => null,
    };

    /// <summary>
    /// Whether the statement containing <paramref name="node"/> null-supplies any
    /// row — the case that makes a NOT NULL column legitimately NULL.
    /// </summary>
    /// <remarks>
    /// Any outer join counts — which side it null-supplies is not asked. Sound
    /// only where <see cref="HasVisibleStatementHead"/> holds: a chain reaching
    /// beyond the statement can join outside what this walks.
    /// </remarks>
    public static bool HasOuterJoin(IOperation node) =>
        HasSpineStep(node, OuterJoinSteps.Contains);

    /// <summary>
    /// Whether the statement containing <paramref name="node"/> can return NULL in
    /// a NOT NULL column: an outer join, or a grouping that adds super-aggregate rows.
    /// </summary>
    /// <remarks>
    /// Read like <see cref="HasOuterJoin"/>, so a <c>ROLLUP</c> silences a predicate
    /// <c>WHERE</c> evaluates before grouping too.
    /// </remarks>
    public static bool HasNullSupplier(IOperation node) =>
        HasSpineStep(
            node,
            name => OuterJoinSteps.Contains(name) || NullSupplyingGroupings.Contains(name));

    private static bool HasSpineStep(IOperation node, System.Func<string, bool> isStep)
    {
        // The outer statement's own spine: every invocation ancestor of the
        // reported node plus each ancestor's receiver chain down to the head.
        HashSet<IOperation> spine = [];
        // A set-operator branch is its own block: the compound's remaining chain is
        // walked through to reach the branch, never for its own (sibling) joins.
        HashSet<IOperation> passThrough = [];
        IOperation top = node;
        CollectSpine(node, spine);
        while (top.Parent is { } parent and not IBlockOperation)
        {
            if (SetOperatorAbove(top) is { } setOperator)
            {
                top = CompoundTail(setOperator);
                CollectLinks(top, setOperator, passThrough);
                continue;
            }

            top = parent;
            CollectSpine(top, spine);
        }

        Stack<IOperation> pending = new();
        pending.Push(top);

        while (pending.Count > 0)
        {
            IOperation current = pending.Pop();

            if (current is IInvocationOperation invocation
                && DialectUsageAnalyzer.IsFromSqlArtisan(
                    invocation.TargetMethod.ContainingAssembly))
            {
                if (passThrough.Contains(invocation))
                {
                    if (invocation.Instance is { } receiver)
                    {
                        pending.Push(receiver);
                    }

                    continue;
                }

                // A chain rooted at its own statement head is a nested subquery
                // — its joins say nothing about the outer statement's shape.
                if (!spine.Contains(invocation) && IsStatementHead(invocation))
                {
                    continue;
                }

                if (isStep(invocation.TargetMethod.Name))
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

    // The invocations from a compound's tail down to (not including) the set
    // operator: the receiver-linked steps of the branches past the reported one.
    private static void CollectLinks(
        IOperation tail, IOperation setOperator, HashSet<IOperation> links)
    {
        IOperation? link = tail;
        while (link is IInvocationOperation invocation && !ReferenceEquals(link, setOperator))
        {
            links.Add(invocation);
            link = invocation.Instance is null ? null : Unwrap(invocation.Instance);
        }
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

    /// <summary>
    /// Whether <paramref name="step"/>'s receiver chain starts at a statement
    /// factory (or a set operator) written in the same expression.
    /// </summary>
    public static bool IsStatementHead(IInvocationOperation step)
    {
        IOperation current = step;

        while (current is IInvocationOperation invocation)
        {
            if (invocation.Instance is null)
            {
                return StatementHeads.Contains(invocation.TargetMethod.Name)
                    && DialectUsageAnalyzer.IsFromSqlArtisan(
                        invocation.TargetMethod.ContainingAssembly);
            }

            current = Unwrap(invocation.Instance);
        }

        return IsSetOperator(current);
    }

    private static bool IsSetOperator(IOperation operation) =>
        operation is IPropertyReferenceOperation property
        && SetOperators.Contains(property.Property.Name)
        && DialectUsageAnalyzer.IsFromSqlArtisan(property.Property.ContainingAssembly);

    // The set-operator property whose receiver is `operation`, through an
    // implicit conversion if one sits between them.
    private static IPropertyReferenceOperation? SetOperatorAbove(IOperation operation)
    {
        IOperation? parent = operation.Parent;
        if (parent is IConversionOperation conversion)
        {
            parent = conversion.Parent;
        }

        return parent is IPropertyReferenceOperation property && IsSetOperator(property)
            ? property
            : null;
    }

    // The outermost invocation of the chain the set operator continues: each
    // step is an invocation whose receiver is the previous step.
    private static IOperation CompoundTail(IOperation setOperator)
    {
        IOperation current = setOperator;
        while (true)
        {
            IOperation? next = current.Parent;
            if (next is IConversionOperation conversion)
            {
                next = conversion.Parent;
            }

            if (next is IInvocationOperation invocation
                && invocation.Instance is { } instance
                && ReferenceEquals(Unwrap(instance), current))
            {
                current = invocation;
                continue;
            }

            return current;
        }
    }

    private static IOperation Unwrap(IOperation operation) =>
        operation is IConversionOperation conversion ? conversion.Operand : operation;
}
