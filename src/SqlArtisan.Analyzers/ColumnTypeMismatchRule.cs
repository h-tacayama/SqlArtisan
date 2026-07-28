using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Reports SQLA0012 when a column is compared to a value of another type category
/// — a text column against a number, say (#362).
/// </summary>
/// <remarks>
/// Categories only, so a <c>numeric(10,2)</c> column against an <c>int</c> is not
/// a mismatch. Neither operand's category being decidable is silence, and an
/// explicit <c>Cast</c> resolves to no category, so it silences the rule too.
/// </remarks>
internal static class ColumnTypeMismatchRule
{
    public static void Check(OperationAnalysisContext context, IBinaryOperation comparison)
    {
        if (!IsComparison(comparison)
            || Side(comparison.LeftOperand) is not { } left
            || Side(comparison.RightOperand) is not { } right
            || left.Category == right.Category)
        {
            return;
        }

        // Naming a column is the whole message, so a comparison between two bound
        // values has nothing to report even when their categories differ.
        if (left.ColumnName is { } leftName)
        {
            Report(context, comparison, leftName, left.Category, right.Category);
        }
        else if (right.ColumnName is { } rightName)
        {
            Report(context, comparison, rightName, right.Category, left.Category);
        }
    }

    private static void Report(
        OperationAnalysisContext context,
        IBinaryOperation comparison,
        string columnName,
        string columnCategory,
        string otherCategory)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ColumnTypeMismatch,
            comparison.Syntax.GetLocation(),
            columnName,
            columnCategory,
            otherCategory));
    }

    private static bool IsComparison(IBinaryOperation comparison) =>
        comparison.OperatorMethod is { } method
        && DialectUsageAnalyzer.IsFromSqlArtisan(method.ContainingAssembly)
        && method.Name is "op_Equality" or "op_Inequality" or "op_LessThan"
            or "op_GreaterThan" or "op_LessThanOrEqual" or "op_GreaterThanOrEqual";

    private static (string Category, string? ColumnName)? Side(IOperation? operand)
    {
        IOperation? node = Unwrap(operand);

        if (node is IPropertyReferenceOperation column)
        {
            return SchemaMetadata.Category(column.Property) is { } category
                ? (category, column.Property.Name)
                : null;
        }

        // Bind carries the value one level down; its own type says nothing.
        if (node is IInvocationOperation call
            && call.TargetMethod.Name == "Bind"
            && DialectUsageAnalyzer.IsFromSqlArtisan(call.TargetMethod.ContainingAssembly)
            && call.Arguments.Length == 1)
        {
            node = Unwrap(call.Arguments[0].Value);
        }

        return node?.Type is { } type && ClrCategory(type) is { } valueCategory
            ? (valueCategory, null)
            : null;
    }

    // Only what a C# type settles. An expression node — a function call, a Cast —
    // lands here as its own type and resolves to nothing, which is the silence the
    // rule wants.
    private static string? ClrCategory(ITypeSymbol type)
    {
        switch (type.SpecialType)
        {
            case SpecialType.System_String:
            case SpecialType.System_Char:
                return ColumnCategories.Text;
            case SpecialType.System_Boolean:
                return ColumnCategories.Boolean;
            case SpecialType.System_SByte:
            case SpecialType.System_Byte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Decimal:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
                return ColumnCategories.Numeric;
            case SpecialType.System_DateTime:
                return ColumnCategories.Temporal;
        }

        if (type is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte })
        {
            return ColumnCategories.Binary;
        }

        return type.ToDisplayString() switch
        {
            "System.DateTimeOffset" or "System.TimeSpan" or "System.DateOnly"
                or "System.TimeOnly" => ColumnCategories.Temporal,
            _ => null,
        };
    }

    private static IOperation? Unwrap(IOperation? operation) =>
        operation is IConversionOperation conversion ? conversion.Operand : operation;
}
