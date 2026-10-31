using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Reports SQLA0303: a comparison, a simple CASE <c>WHEN</c>, or an <c>IN</c>, <c>BETWEEN</c>
/// or <c>LIKE</c> predicate against a written NULL that is never true (#742).
/// </summary>
/// <remarks>
/// <c>==</c> also spells a <c>SET</c> assignment, so it is judged only where it is read as a
/// <c>SqlCondition</c>; a <c>SET</c> rejects <c>!=</c> at the call and takes no ordering
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
            BinaryOperatorKind.Equals => TestWith("IsNull"),
            BinaryOperatorKind.NotEquals => TestWith("IsNotNull"),
            _ => TestWith("IsNull or IsNotNull"),
        };
        Report(context, binary, token, remedy);
    }

    // IN reports only when every value is NULL: one other value can still match.
    public static void CheckPredicate(OperationAnalysisContext context, IInvocationOperation call)
    {
        IMethodSymbol method = call.TargetMethod;
        if (method.ContainingType.Name != "SqlExpression"
            || !DialectUsageAnalyzer.IsFromSqlArtisan(method.ContainingAssembly))
        {
            return;
        }

        switch (method.Name)
        {
            case "NotIn" when ListValues(call) is { } values && values.Exists(IsSqlNull):
                Report(
                    context,
                    call,
                    "NOT IN",
                    values.TrueForAll(IsSqlNull)
                        ? TestWith("IsNotNull")
                        : "drop the NULL from the list");
                break;
            case "In" when ListValues(call) is { Count: > 0 } values
                && values.TrueForAll(IsSqlNull):
                Report(context, call, "IN", TestWith("IsNull"));
                break;
            case "Between" when call.Arguments.Length == 2 && CountNullArguments(call) > 0:
                Report(
                    context,
                    call,
                    "BETWEEN",
                    CountNullArguments(call) == 2
                        ? TestWith("IsNull")
                        : "compare the other bound with >= or <=");
                break;
            case "Like" or "NotLike" when call.Arguments.Length == 1
                && IsSqlNull(call.Arguments[0].Value):
                Report(
                    context,
                    call,
                    method.Name == "Like" ? "LIKE" : "NOT LIKE",
                    TestWith("IsNull or IsNotNull"));
                break;
        }
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
            || !IsSqlNull(invocation.Arguments[0].Value, bound: false))
        {
            return;
        }

        Report(context, invocation, "WHEN NULL", TestWith("a searched When(x.IsNull)"));
    }

    private static int CountNullArguments(IInvocationOperation call)
    {
        int count = 0;
        foreach (IArgumentOperation argument in call.Arguments)
        {
            if (IsSqlNull(argument.Value))
            {
                count++;
            }
        }

        return count;
    }

    private static string TestWith(string test) => "test for NULL with " + test;

    private static void Report(
        OperationAnalysisContext context, IOperation operation, string token, string remedy) =>
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.NullComparison, operation.Syntax.GetLocation(), token, remedy));

    // The values of a params list written at the call; an array built elsewhere is unread.
    private static List<IOperation>? ListValues(IInvocationOperation call) =>
        call.Arguments.Length == 1
        && call.TargetMethod.Parameters[0].Type is IArrayTypeSymbol
        && call.Arguments[0].Value is IArrayCreationOperation { Initializer: { } list }
            ? [.. list.ElementValues]
            : null;

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

    // Not a bound NULL in a simple CASE: Oracle XE 21.3.0 binds an untyped BindNull() as
    // text and rejects the CASE (SimpleCaseWhenUntypedBindNull_IsRejectedByTheEngine).
    private static bool IsSqlNull(IOperation operand) => IsSqlNull(operand, bound: true);

    private static bool IsSqlNull(IOperation operand, bool bound)
    {
        while (operand is IConversionOperation { IsImplicit: true } conversion)
        {
            operand = conversion.Operand;
        }

        // Untyped BindNull() only, which the twins run in each position but a simple CASE;
        // what a typed one does turns on the type it names.
        return operand switch
        {
            IPropertyReferenceOperation
            {
                Property: { Name: "Null", ContainingType.Name: "Sql" } property,
            } => DialectUsageAnalyzer.IsFromSqlArtisan(property.ContainingAssembly),
            IInvocationOperation
            {
                TargetMethod:
                {
                    Name: "BindNull", ContainingType.Name: "Sql", Parameters.Length: 0,
                } bindNull,
            } => bound && DialectUsageAnalyzer.IsFromSqlArtisan(bindNull.ContainingAssembly),
            _ => false,
        };
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
