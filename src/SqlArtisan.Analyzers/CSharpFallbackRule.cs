using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Reports SQLA0301 / SQLA0302: an operand order or string form that C# resolves
/// without SqlArtisan, so a SqlArtisan argument binds a <c>bool</c> or a type name
/// instead of building SQL (ADR 0023).
/// </summary>
/// <remarks>
/// Reported only where the value flows straight into an argument of a SqlArtisan
/// member: the same C# elsewhere (a log line, a reference check) is correct code,
/// and ADR 0003 keeps the analyzer silent where it cannot prove the hazard.
/// </remarks>
internal static class CSharpFallbackRule
{
    public static void CheckReferenceEquality(
        OperationAnalysisContext context,
        IBinaryOperation binary)
    {
        if (binary.OperatorMethod is not null
            || binary.OperatorKind
                is not (BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals)
            || !(IsQueryObject(Unconverted(binary.LeftOperand).Type)
                || IsQueryObject(Unconverted(binary.RightOperand).Type))
            || !FlowsIntoSqlArtisanArgument(binary))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ReferenceEqualityBound,
            binary.Syntax.GetLocation()));
    }

    // SqlArtisan's `+` takes a SqlExpression or ExpressionAlias operand, so `"x" + p` over
    // any other query object is C# concatenation through ToString().
    public static void CheckConcatenation(OperationAnalysisContext context, IBinaryOperation binary)
    {
        if (binary.OperatorMethod is not null
            || binary.OperatorKind != BinaryOperatorKind.Add
            || binary.Type?.SpecialType != SpecialType.System_String
            || !FlowsIntoSqlArtisanArgument(binary))
        {
            return;
        }

        foreach (IOperation operand in new[] { binary.LeftOperand, binary.RightOperand })
        {
            if (IsQueryObject(Unconverted(operand).Type))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.QueryObjectAsText,
                    operand.Syntax.GetLocation(),
                    Unconverted(operand).Type!.Name));
            }
        }
    }

    public static void CheckInterpolation(
        OperationAnalysisContext context,
        IInterpolatedStringOperation interpolated)
    {
        if (!FlowsIntoSqlArtisanArgument(interpolated))
        {
            return;
        }

        foreach (IInterpolatedStringContentOperation part in interpolated.Parts)
        {
            if (part is IInterpolationOperation hole
                && IsQueryObject(Unconverted(hole.Expression).Type))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.QueryObjectAsText,
                    hole.Syntax.GetLocation(),
                    Unconverted(hole.Expression).Type!.Name));
            }
        }
    }

    // A SqlArtisan query part or query: what renders as SQL only through SqlArtisan.
    // A built SqlStatement is not one — its ToString is the SQL text by design.
    private static bool IsQueryObject(ITypeSymbol? type)
    {
        if (type is null || type.TypeKind == TypeKind.Error)
        {
            return false;
        }

        if (type is ITypeParameterSymbol parameter)
        {
            return parameter.ConstraintTypes.Any(IsQueryObject);
        }

        for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (IsSqlArtisanType(current, "SqlPart"))
            {
                return true;
            }
        }

        foreach (INamedTypeSymbol contract in type.AllInterfaces)
        {
            if (IsQueryContract(contract))
            {
                return true;
            }
        }

        return type.TypeKind == TypeKind.Interface && IsQueryContract(type);
    }

    // IIncompleteExpression marks a pending node (RowNumber() before .Over(...)), which
    // is no SqlPart yet but formats as its type name all the same.
    private static bool IsQueryContract(ITypeSymbol type) =>
        IsSqlArtisanType(type, "ISubquery")
        || IsSqlArtisanType(type, "ISqlBuilder")
        || IsSqlArtisanType(type, "IIncompleteExpression");

    private static bool IsSqlArtisanType(ITypeSymbol type, string name) =>
        type.Name == name
        && DialectUsageAnalyzer.IsFromSqlArtisan(type.ContainingAssembly);

    private static IOperation Unconverted(IOperation operation)
    {
        while (operation is IConversionOperation { IsImplicit: true } conversion)
        {
            operation = conversion.Operand;
        }

        return operation;
    }

    // The value reaches the argument through implicit conversions, an enclosing string
    // concatenation and, for a params tail, the compiler's own array; anything else (a
    // local, a ternary) stops the walk.
    private static bool FlowsIntoSqlArtisanArgument(IOperation operation)
    {
        IOperation? current = operation.Parent;
        while (current is IConversionOperation { IsImplicit: true }
            or IArrayInitializerOperation
            or IArrayCreationOperation { IsImplicit: true }
            or IBinaryOperation
            {
                OperatorMethod: null,
                OperatorKind: BinaryOperatorKind.Add,
                Type.SpecialType: SpecialType.System_String,
            })
        {
            current = current.Parent;
        }

        return current is IArgumentOperation { Parent: { } invoked }
            && invoked switch
            {
                IInvocationOperation invocation => DialectUsageAnalyzer.IsFromSqlArtisan(
                    invocation.TargetMethod.ContainingAssembly),
                IObjectCreationOperation creation =>
                    creation.Constructor is { } constructor
                    && DialectUsageAnalyzer.IsFromSqlArtisan(constructor.ContainingAssembly),
                _ => false,
            };
    }
}
