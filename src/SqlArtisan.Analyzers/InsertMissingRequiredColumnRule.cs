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
        if (Unwrap(insert.Arguments[0].Value).Type is not { } table
            || ListedColumns(insert.Arguments[1]) is not { } listed)
        {
            return;
        }

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

    // Null means the list could not be read in full — a column array built
    // elsewhere, or an item that is not a table-class property.
    private static List<IPropertySymbol>? ListedColumns(IArgumentOperation columns)
    {
        if (columns.ArgumentKind != ArgumentKind.ParamArray
            || columns.Value is not IArrayCreationOperation { Initializer: { } items })
        {
            return null;
        }

        List<IPropertySymbol> listed = [];

        foreach (IOperation item in items.ElementValues)
        {
            if (Unwrap(item) is not IPropertyReferenceOperation column)
            {
                return null;
            }

            listed.Add(column.Property);
        }

        return listed;
    }

    private static IOperation Unwrap(IOperation operation) =>
        operation is IConversionOperation conversion ? conversion.Operand : operation;
}
