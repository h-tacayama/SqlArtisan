using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Reports SQLA0205 when a column is compared to a value of another type category
/// — a text column against a number, say (#362).
/// </summary>
/// <remarks>
/// Categories only, so a <c>numeric(10,2)</c> column against an <c>int</c> is not
/// a mismatch. Neither operand's category being decidable is silence, and an
/// explicit <c>Cast</c> resolves to no category, so it silences the rule too.
/// </remarks>
internal static class TypeCategoryMismatchRule
{
    // Where == spells an assignment rather than a comparison. SET coerces by
    // rules fixed per engine and cannot change which rows match, and "cast one
    // side" would name a side that is not there.
    private static readonly HashSet<string> AssignmentSteps =
        ["DoUpdateSet", "OnDuplicateKeyUpdate", "Set", "ThenUpdateSet"];

    public static void Check(OperationAnalysisContext context, IBinaryOperation comparison)
    {
        if (!IsComparison(comparison)
            || !IsComparisonPosition(comparison)
            || Side(comparison.LeftOperand) is not { } left
            || Side(comparison.RightOperand) is not { } right
            || Compatible(left.Category, right.Category))
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
        TypeCategory columnCategory,
        TypeCategory otherCategory)
    {
        // The member names read as prose only in lower case.
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.TypeCategoryMismatch,
            comparison.Syntax.GetLocation(),
            columnName,
            columnCategory.ToString().ToLowerInvariant(),
            otherCategory.ToString().ToLowerInvariant()));
    }

    // A truth value and a number are one category in practice: T-SQL offers no
    // boolean literal, so `bit = 1` is its only spelling, and MySQL's BOOLEAN is
    // TINYINT(1). Only PostgreSQL rejects the pair, and it does so loudly.
    private static bool Compatible(TypeCategory left, TypeCategory right) =>
        left == right
        || (IsTruthy(left) && IsTruthy(right));

    private static bool IsTruthy(TypeCategory category) =>
        category is TypeCategory.Boolean or TypeCategory.Numeric;

    // The first enclosing SqlArtisan step decides, and a condition built apart
    // from its clause is left alone rather than guessed at — the same trade
    // UnusableIndexPredicateRule makes to find its filtering clause.
    private static bool IsComparisonPosition(IOperation node)
    {
        IOperation current = node;

        while (current.Parent is { } parent and not IBlockOperation)
        {
            if (parent is IInvocationOperation step
                && DialectUsageAnalyzer.IsFromSqlArtisan(step.TargetMethod.ContainingAssembly)
                && step.Instance is not null)
            {
                return !AssignmentSteps.Contains(step.TargetMethod.Name);
            }

            current = parent;
        }

        return false;
    }

    private static bool IsComparison(IBinaryOperation comparison) =>
        comparison.OperatorMethod is { } method
        && DialectUsageAnalyzer.IsFromSqlArtisan(method.ContainingAssembly)
        && method.Name is "op_Equality" or "op_Inequality" or "op_LessThan"
            or "op_GreaterThan" or "op_LessThanOrEqual" or "op_GreaterThanOrEqual";

    private static (TypeCategory Category, string? ColumnName)? Side(IOperation? operand)
    {
        IOperation? node = Unwrap(operand);

        // A property carrying no category is not necessarily a column: it falls
        // through to be judged by its C# type, the way a field or local is.
        if (node is IPropertyReferenceOperation column
            && SchemaMetadata.Category(column.Property) is { } category)
        {
            return (category, column.Property.Name);
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
    private static TypeCategory? ClrCategory(ITypeSymbol type)
    {
        // int? carries exactly the type int does; a DTO's nullable field is one of
        // the commonest ways a value reaches a comparison.
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
        {
            type = nullable.TypeArguments[0];
        }

        switch (type.SpecialType)
        {
            case SpecialType.System_String:
            case SpecialType.System_Char:
                return TypeCategory.Text;
            case SpecialType.System_Boolean:
                return TypeCategory.Boolean;
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
                return TypeCategory.Numeric;
            case SpecialType.System_DateTime:
                return TypeCategory.Temporal;
        }

        if (type is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte })
        {
            return TypeCategory.Binary;
        }

        return type.ToDisplayString() switch
        {
            "System.DateTimeOffset" or "System.TimeSpan" or "System.DateOnly"
                or "System.TimeOnly" => TypeCategory.Temporal,
            _ => null,
        };
    }

    private static IOperation? Unwrap(IOperation? operation) =>
        operation is IConversionOperation conversion ? conversion.Operand : operation;
}
