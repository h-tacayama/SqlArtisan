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
            || FiltersOutNulls(notIn.Arguments[0].Value, item.Property)
            || HidesACondition(notIn.Arguments[0].Value))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.NotInNullableSubquery,
            notIn.Syntax.GetLocation(),
            item.Property.Name));
    }

    // The documented remediation is `.Where(col.IsNotNull)`, so the rule must go
    // quiet when it sees it — or its `Not(col.IsNull)` twin, since IS NULL is the
    // one predicate three-valued logic never leaves UNKNOWN. Presence anywhere in
    // the subquery is enough: a filter that does not actually exclude the NULLs
    // (under an OR, in a different clause) yields a false negative, never a false
    // positive.
    private static bool FiltersOutNulls(IOperation subquery, IPropertySymbol column)
    {
        Stack<IOperation> pending = new();
        pending.Push(subquery);

        while (pending.Count > 0)
        {
            IOperation current = pending.Pop();

            if (IsNotNullFilter(current, column))
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

    private static bool IsNotNullFilter(IOperation candidate, IPropertySymbol column) =>
        candidate is IPropertyReferenceOperation { Property.Name: "IsNotNull" } direct
            ? MatchesColumn(direct.Instance, column)
            : candidate is IInvocationOperation { TargetMethod.Name: "Not" } negation
                && DialectUsageAnalyzer.IsFromSqlArtisan(negation.TargetMethod.ContainingAssembly)
                && negation.Arguments.Length == 1
                && Unwrap(negation.Arguments[0].Value) is IPropertyReferenceOperation
                { Property.Name: "IsNull" } negatedNull
                && MatchesColumn(negatedNull.Instance, column);

    private static bool MatchesColumn(IOperation? instance, IPropertySymbol column) =>
        Unwrap(instance!) is IPropertyReferenceOperation filtered
        && SymbolEqualityComparer.Default.Equals(filtered.Property, column);

    // The remediation reaches the subquery held in a local or a field as often as
    // written inline, and there the walk above sees only the reference. A
    // predicate this rule cannot read might already filter the NULLs out.
    private static bool HidesACondition(IOperation subquery)
    {
        Stack<IOperation> pending = new();
        pending.Push(subquery);

        while (pending.Count > 0)
        {
            IOperation current = pending.Pop();

            if (FluentChain.IsCondition(current.Type) && !IsReadable(current))
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

    // A condition the walk can descend into: one the core itself builds here.
    // IsNotNull/IsNull is readable only past a receiver declared on a table
    // reference — an arbitrary DbColumn-typed property is structurally a
    // property reference too, but its value is opaque the same way a local or a
    // field's is, so it must not pass this check either.
    private static bool IsReadable(IOperation condition) => condition switch
    {
        IPropertyReferenceOperation { Property.Name: "IsNotNull" or "IsNull" } nullCheck =>
            DialectUsageAnalyzer.IsFromSqlArtisan(nullCheck.Property.ContainingAssembly)
                && Unwrap(nullCheck.Instance!) is IPropertyReferenceOperation receiver
                && FluentChain.IsTableReference(receiver.Property.ContainingType),
        IPropertyReferenceOperation property =>
            DialectUsageAnalyzer.IsFromSqlArtisan(property.Property.ContainingAssembly),
        IInvocationOperation call =>
            DialectUsageAnalyzer.IsFromSqlArtisan(call.TargetMethod.ContainingAssembly),
        IBinaryOperation or IConversionOperation or IObjectCreationOperation => true,
        _ => false,
    };

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
