using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Reports SQLA0008 for <c>NOT IN (subquery)</c> whose selected column is
/// nullable: one NULL in the subquery makes the predicate NULL for every row,
/// so the statement silently returns nothing (#266).
/// </summary>
/// <remarks>
/// Every walk fails toward silence (ADR 0003): a subquery held in a variable, a
/// select list this rule cannot read, or a select item that is not a column all
/// yield a false negative, never a false positive.
/// </remarks>
internal static class NotInNullableSubqueryRule
{
    public static void Check(OperationAnalysisContext context, IInvocationOperation notIn)
    {
        if (SoleSelectItem(Head(notIn.Arguments[0].Value)) is not IPropertyReferenceOperation item
            || SchemaMetadata.Fact(item, SchemaMetadata.NullableArgument) is not true
            || FiltersOutNulls(notIn.Arguments[0].Value, item.Property))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.NotInNullableSubquery,
            notIn.Syntax.GetLocation(),
            item.Property.Name));
    }

    // The documented remediation is `.Where(col.IsNotNull)`, so the rule must go
    // quiet when it sees it. Presence anywhere in the subquery is enough: an
    // IsNotNull that does not actually exclude the NULLs (under an OR, in a
    // different clause) yields a false negative, never a false positive.
    private static bool FiltersOutNulls(IOperation subquery, IPropertySymbol column)
    {
        Stack<IOperation> pending = new();
        pending.Push(subquery);

        while (pending.Count > 0)
        {
            IOperation current = pending.Pop();

            if (current is IPropertyReferenceOperation { Property.Name: "IsNotNull" } filter
                && Unwrap(filter.Instance!) is IPropertyReferenceOperation filtered
                && SymbolEqualityComparer.Default.Equals(filtered.Property, column))
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

    // The subquery argument is the chain's tail, so the select list is found by
    // walking receivers down to the static head the chain started from.
    private static IInvocationOperation? Head(IOperation subquery)
    {
        IOperation current = Unwrap(subquery);

        while (current is IInvocationOperation invocation)
        {
            if (invocation.Instance is null)
            {
                return DialectUsageAnalyzer.IsFromSqlArtisan(invocation.TargetMethod.ContainingAssembly)
                    ? invocation
                    : null;
            }

            current = Unwrap(invocation.Instance);
        }

        return null;
    }

    // Only a single-column select list can be the one NOT IN compares against;
    // any other shape is left alone.
    private static IOperation? SoleSelectItem(IInvocationOperation? select)
    {
        if (select?.TargetMethod.Name != "Select")
        {
            return null;
        }

        foreach (IArgumentOperation argument in select.Arguments)
        {
            if (argument.ArgumentKind == ArgumentKind.ParamArray
                && argument.Value is IArrayCreationOperation { Initializer: { } items }
                && items.ElementValues.Length == 1)
            {
                return Unwrap(items.ElementValues[0]);
            }
        }

        return null;
    }

    private static IOperation Unwrap(IOperation operation) =>
        operation is IConversionOperation conversion ? conversion.Operand : operation;
}
