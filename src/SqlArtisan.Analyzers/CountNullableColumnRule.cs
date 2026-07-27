using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Reports SQLA0010 for <c>COUNT(column)</c> on a column the schema declares
/// nullable — a count of values where <c>COUNT(*)</c> counts rows (#266).
/// </summary>
/// <remarks>
/// Counting non-NULL values is a legitimate thing to write, so this fires on
/// correct code; it ships disabled by default as advice, not as a defect
/// alongside the schema warnings.
/// </remarks>
internal static class CountNullableColumnRule
{
    public static void Check(OperationAnalysisContext context, IInvocationOperation count)
    {
        // The parameter is object, so the column arrives behind a boxing conversion.
        IOperation argument = count.Arguments[0].Value;
        if (argument is IConversionOperation conversion)
        {
            argument = conversion.Operand;
        }

        // Past an outer join, counting the column is how you count matched rows —
        // COUNT(*) counts the unmatched ones too, so the advice would be wrong.
        if (argument is not IPropertyReferenceOperation column
            || SchemaMetadata.Fact(column, SchemaMetadata.NullableArgument) is not true
            || !FluentChain.HasVisibleStatementHead(count)
            || FluentChain.HasOuterJoin(count))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.CountNullableColumn,
            count.Syntax.GetLocation(),
            column.Property.Name));
    }
}
