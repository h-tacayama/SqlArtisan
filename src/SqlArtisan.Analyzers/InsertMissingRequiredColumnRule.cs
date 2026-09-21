using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Reports SQLA0202 when an <c>INSERT</c>'s column list omits a column the
/// schema declares NOT NULL with no default — a row the engine rejects unless
/// something outside the catalog, such as a trigger, supplies the value (#266).
/// </summary>
/// <remarks>
/// Both facts must be known before a column counts as required, and the whole
/// statement is skipped when any listed column cannot be resolved: a column
/// this rule failed to read would otherwise look omitted (ADR 0003).
/// </remarks>
internal static class InsertMissingRequiredColumnRule
{
    public static void Check(OperationAnalysisContext context, IInvocationOperation insert)
    {
        // By parameter name: Roslyn orders Arguments as written, so a named
        // argument can put the column list first.
        if (FindArgument(insert, "table") is not { } tableArgument
            || FindArgument(insert, "columns") is not { } columnsArgument
            || Unwrap(tableArgument.Value).Type is not { } table
            || ListedColumns(columnsArgument) is not { } listed)
        {
            return;
        }

        ReportMissing(context, insert, table, listed);
    }

    // The Set form emits the assigned columns as its list, so the assignments'
    // left sides are read the way the column array is; reported at the head.
    public static void CheckSet(
        OperationAnalysisContext context, IInvocationOperation set, IInvocationOperation insert)
    {
        if (FindArgument(insert, "table") is not { } tableArgument
            || Unwrap(tableArgument.Value).Type is not { } table
            || FindArgument(set, "assignments") is not { } assignments
            || AssignedColumns(assignments) is not { } listed)
        {
            return;
        }

        ReportMissing(context, insert, table, listed);
    }

    // The InsertInto(table) head under a Set(...) step, or null for any other Set.
    public static IInvocationOperation? BareInsertHead(IInvocationOperation set) =>
        set.Instance is { } receiver
            && Unwrap(receiver) is IInvocationOperation head
            && head.TargetMethod.Name == "InsertInto"
            && head.Arguments.Length == 1
            && DialectUsageAnalyzer.IsFromSqlArtisan(head.TargetMethod.ContainingAssembly)
            ? head
            : null;

    private static void ReportMissing(
        OperationAnalysisContext context,
        IInvocationOperation insert,
        ITypeSymbol table,
        List<IPropertySymbol> listed)
    {
        foreach (IPropertySymbol column in table.GetMembers().OfType<IPropertySymbol>())
        {
            if (listed.Contains(column, SymbolEqualityComparer.Default)
                || SchemaMetadata.Fact(column, SchemaMetadata.NullableArgument) is not false
                || SchemaMetadata.Fact(column, SchemaMetadata.HasDefaultArgument) is not false)
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.InsertMissingRequiredColumn,
                insert.Syntax.GetLocation(),
                column.Name));
        }
    }

    // Null under the same rule as ListedColumns: an assignment whose left side is
    // not a table-class property, or a list built elsewhere, is unread.
    private static List<IPropertySymbol>? AssignedColumns(IArgumentOperation assignments)
    {
        if (Elements(assignments) is not { } items)
        {
            return null;
        }

        List<IPropertySymbol> listed = [];

        foreach (IOperation item in items)
        {
            if (Unwrap(item) is not IBinaryOperation { OperatorMethod: not null } assignment
                || Unwrap(assignment.LeftOperand) is not IPropertyReferenceOperation column)
            {
                return null;
            }

            listed.Add(column.Property);
        }

        return listed;
    }

    // Null means the list could not be read in full — a column array built elsewhere,
    // or an item that is not a table-class property.
    private static List<IPropertySymbol>? ListedColumns(IArgumentOperation columns)
    {
        if (Elements(columns) is not { } items)
        {
            return null;
        }

        List<IPropertySymbol> listed = [];

        foreach (IOperation item in items)
        {
            if (Unwrap(item) is not IPropertyReferenceOperation column)
            {
                return null;
            }

            listed.Add(column.Property);
        }

        return listed;
    }

    // An inline params/array list, or a collection expression's children (the
    // pinned Roslyn types it as an array with no operation of its own, as
    // IdentifierLengthRule.Elements reads it); anything else is built elsewhere.
    private static IEnumerable<IOperation>? Elements(IArgumentOperation argument) =>
        Unwrap(argument.Value) switch
        {
            IArrayCreationOperation { Initializer: { } initializer } => initializer.ElementValues,
            { Type: IArrayTypeSymbol } and not (IInvocationOperation or ILocalReferenceOperation
                or IParameterReferenceOperation or IFieldReferenceOperation
                or IPropertyReferenceOperation or IConversionOperation) and { } value =>
                value.ChildOperations,
            _ => null,
        };

    private static IArgumentOperation? FindArgument(
        IInvocationOperation invocation, string parameterName)
    {
        foreach (IArgumentOperation argument in invocation.Arguments)
        {
            if (argument.Parameter?.Name == parameterName)
            {
                return argument;
            }
        }

        return null;
    }

    private static IOperation Unwrap(IOperation operation) =>
        operation is IConversionOperation conversion ? conversion.Operand : operation;
}
