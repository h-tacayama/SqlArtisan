using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Reports SQLA0303: a comparison or a simple CASE <c>WHEN</c> against <c>Sql.Null</c>,
/// which is never true, so the predicate matches no row and the branch is never taken.
/// </summary>
/// <remarks>
/// <c>==</c> also spells a <c>SET</c> assignment, so it is judged only where it is read as a
/// <c>SqlCondition</c>; a <c>SET</c> rejects <c>!=</c> at <c>Build()</c> and takes no ordering
/// comparison, so those are judged wherever they appear.
/// </remarks>
internal static class NullComparisonRule
{
    public static void CheckComparison(OperationAnalysisContext context, IBinaryOperation binary)
    {
        if (binary.OperatorMethod is not { } method
            || !DialectUsageAnalyzer.IsFromSqlArtisan(method.ContainingAssembly)
            || Token(binary.OperatorKind) is not { } token
            || !(IsSqlNull(binary.LeftOperand) || IsSqlNull(binary.RightOperand))
            || (binary.OperatorKind == BinaryOperatorKind.Equals && !IsReadAsCondition(binary)))
        {
            return;
        }

        string remedy = binary.OperatorKind switch
        {
            BinaryOperatorKind.Equals => "IsNull",
            BinaryOperatorKind.NotEquals => "IsNotNull",
            _ => "IsNull or IsNotNull",
        };
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.NullComparison, binary.Syntax.GetLocation(), token, remedy));
    }

    public static void CheckSimpleCaseWhen(
        OperationAnalysisContext context,
        IInvocationOperation invocation)
    {
        IMethodSymbol method = invocation.TargetMethod;
        if (method.Name != "When"
            || method.ContainingType.Name != "Sql"
            || !DialectUsageAnalyzer.IsFromSqlArtisan(method.ContainingAssembly)
            || method.Parameters.Length != 1
            || method.Parameters[0].Type.SpecialType != SpecialType.System_Object
            || !IsSqlNull(invocation.Arguments[0].Value))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.NullComparison,
            invocation.Syntax.GetLocation(),
            "WHEN NULL",
            "a searched When(x.IsNull)"));
    }

    private static string? Token(BinaryOperatorKind kind) => kind switch
    {
        BinaryOperatorKind.Equals => "=",
        BinaryOperatorKind.NotEquals => "<>",
        BinaryOperatorKind.LessThan => "<",
        BinaryOperatorKind.LessThanOrEqual => "<=",
        BinaryOperatorKind.GreaterThan => ">",
        BinaryOperatorKind.GreaterThanOrEqual => ">=",
        _ => null,
    };

    private static bool IsSqlNull(IOperation operand)
    {
        while (operand is IConversionOperation { IsImplicit: true } conversion)
        {
            operand = conversion.Operand;
        }

        return operand is IPropertyReferenceOperation
        {
            Property: { Name: "Null", ContainingType.Name: "Sql" } property,
        }
            && DialectUsageAnalyzer.IsFromSqlArtisan(property.ContainingAssembly);
    }

    private static bool IsReadAsCondition(IOperation operation)
    {
        ITypeSymbol? type = operation.Type;
        while (operation.Parent is IConversionOperation { IsImplicit: true }
            or IConditionalOperation)
        {
            if (operation.Parent is IConditionalOperation conditional
                && conditional.Condition == operation)
            {
                break;
            }

            operation = operation.Parent;
            type = operation.Type;
        }

        return type is { Name: "SqlCondition" }
            && DialectUsageAnalyzer.IsFromSqlArtisan(type.ContainingAssembly);
    }
}
