using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Reports SQLA0206 when a column the table class declares text is an operand of
/// <c>SqlExpression</c>'s <c>+</c>, on an engine whose <c>+</c> does not concatenate (#742).
/// </summary>
/// <remarks>
/// A string or char constant beside it is SQLA0104's, so one <c>+</c> reports once; an
/// interval beside it is MySQL's date arithmetic, which a text date column takes.
/// </remarks>
internal static class TextColumnPlusRule
{
    public static void Check(
        OperationAnalysisContext context,
        DialectTargetSet targets,
        IOperation[] operands)
    {
        string? column = null;

        foreach (IOperation operand in operands)
        {
            IOperation value = Unwrap(operand);
            if (ArgumentValueValidityRule.StringConstant(operand) is not null
                || IsInterval(value.Type))
            {
                return;
            }

            if (column is null
                && value is IPropertyReferenceOperation reference
                && SchemaMetadata.Category(reference.Property) == TypeCategory.Text)
            {
                column = reference.Property.Name;
            }
        }

        if (column is null)
        {
            return;
        }

        List<string> invalidOn = [];

        foreach (TargetDbms dbms in targets.Members)
        {
            if (ArgumentValueValidity.ConcatenatesNoStringPlusOperand(dbms))
            {
                invalidOn.Add(TargetDbmsNames.Display(dbms));
            }
        }

        if (invalidOn.Count > 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.TextColumnPlus,
                context.Operation.Syntax.GetLocation(),
                column,
                TargetDbmsNames.JoinDisplayNames(invalidOn)));
        }
    }

    private static bool IsInterval(ITypeSymbol? type) =>
        type is { Name: "IntervalExpression" }
        && DialectUsageAnalyzer.IsFromSqlArtisan(type.ContainingAssembly);

    private static IOperation Unwrap(IOperation operation) =>
        operation is IConversionOperation conversion ? conversion.Operand : operation;
}
