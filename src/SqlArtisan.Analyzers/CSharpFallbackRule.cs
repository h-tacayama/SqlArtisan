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
/// Reported only where the value flows straight into a SqlArtisan member's argument
/// or operator's operand: the same C# elsewhere (a log line, a reference check) is
/// correct code, and ADR 0003 keeps the analyzer silent where it cannot prove the hazard.
/// </remarks>
internal static class CSharpFallbackRule
{
    public static void CheckReferenceEquality(
        OperationAnalysisContext context,
        IBinaryOperation binary)
    {
        ITypeSymbol? left = Unconverted(binary.LeftOperand).Type;
        ITypeSymbol? right = Unconverted(binary.RightOperand).Type;
        if (binary.OperatorMethod is not null
            || binary.OperatorKind
                is not (BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals)
            || !(IsSqlArtisanObject(left) || IsSqlArtisanObject(right))
            // Two tables or two sequences: no operand order makes that a SQL comparison.
            || context.Compilation.GetTypeByMetadataName("SqlArtisan.SqlExpression")
                is not { } expression
            || !(CanHoldExpression(left, expression) || CanHoldExpression(right, expression))
            // A null check is meant as C#: no operand order makes it SQL.
            || IsNullConstant(binary.LeftOperand)
            || IsNullConstant(binary.RightOperand)
            || !TryFindSqlArtisanSink(binary, out ITypeSymbol? parameterType)
            // A bool parameter (ConditionIf's `when`) takes the C# test on purpose.
            || parameterType?.SpecialType == SpecialType.System_Boolean
            // CS0019 already rejects it: nothing compiles, so nothing binds.
            || binary.SemanticModel?.GetDiagnostics(binary.Syntax.Span, context.CancellationToken)
                .Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error) != false)
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
            || !TryFindSqlArtisanSink(binary, out _))
        {
            return;
        }

        foreach (IOperation operand in new[] { binary.LeftOperand, binary.RightOperand })
        {
            if (FormatsAsTypeName(operand))
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
        if (!TryFindSqlArtisanSink(interpolated, out _))
        {
            return;
        }

        foreach (IInterpolatedStringContentOperation part in interpolated.Parts)
        {
            if (part is IInterpolationOperation hole && FormatsAsTypeName(hole.Expression))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.QueryObjectAsText,
                    hole.Syntax.GetLocation(),
                    Unconverted(hole.Expression).Type!.Name));
            }
        }
    }

    // A reference type from SqlArtisan, or one deriving from or implementing one.
    private static bool IsSqlArtisanObject(ITypeSymbol? type)
    {
        if (type is ITypeParameterSymbol parameter)
        {
            return parameter.ConstraintTypes.Any(IsSqlArtisanObject);
        }

        if (type is null || type.TypeKind == TypeKind.Error || !type.IsReferenceType)
        {
            return false;
        }

        for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (IsFromSqlArtisan(current))
            {
                return true;
            }
        }

        return type.AllInterfaces.Any(IsFromSqlArtisan);
    }

    // Provable only when no ToString override can hide behind the static type: a sealed
    // type, or an object a SqlArtisan member created. A DbTableBase- or SqlPart-typed
    // value may hold a user table class that overrides it (ADR 0003).
    private static bool FormatsAsTypeName(IOperation value)
    {
        IOperation operation = Unconverted(value);
        if (operation.Type is not { } type
            || type is ITypeParameterSymbol
            || !IsSqlArtisanObject(type)
            || OverridesToString(type))
        {
            return false;
        }

        return type.IsSealed || operation switch
        {
            IInvocationOperation invocation => IsSqlArtisanFactory(invocation.TargetMethod),
            IPropertyReferenceOperation property =>
                IsSqlArtisanFactory(property.Property.GetMethod),
            IObjectCreationOperation creation => creation.Constructor is { } constructor
                && IsFromSqlArtisan(constructor.ContainingType),
            _ => false,
        };
    }

    // SqlStatement's override returns its SQL text, by design.
    private static bool OverridesToString(ITypeSymbol type)
    {
        for (ITypeSymbol? current = type;
            current is not null && current.SpecialType != SpecialType.System_Object;
            current = current.BaseType)
        {
            if (current.GetMembers(nameof(ToString)).OfType<IMethodSymbol>()
                .Any(method => method.IsOverride && method.Parameters.IsEmpty))
            {
                return true;
            }
        }

        return false;
    }

    // A generic return can hand back the caller's own object; any other is SqlArtisan's.
    private static bool IsSqlArtisanFactory(IMethodSymbol? method) =>
        method is not null
        && DialectUsageAnalyzer.IsFromSqlArtisan(method.ContainingAssembly)
        && method.OriginalDefinition.ReturnType is not ITypeParameterSymbol;

    private static bool IsFromSqlArtisan(ITypeSymbol type) =>
        DialectUsageAnalyzer.IsFromSqlArtisan(type.ContainingAssembly);

    // An interface counts: a SqlExpression subclass may implement it.
    private static bool CanHoldExpression(ITypeSymbol? type, INamedTypeSymbol expression)
    {
        if (type is ITypeParameterSymbol parameter)
        {
            return parameter.ConstraintTypes.All(
                constraint => CanHoldExpression(constraint, expression));
        }

        if (type is null || type.TypeKind == TypeKind.Interface)
        {
            return type is not null;
        }

        for (ITypeSymbol? current = expression; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, type))
            {
                return true;
            }
        }

        for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, expression))
            {
                return true;
            }
        }

        return false;
    }

    // `null`, `default`, a null constant: any compile-time null.
    private static bool IsNullConstant(IOperation operand) =>
        Unconverted(operand).ConstantValue is { HasValue: true, Value: null };

    private static IOperation Unconverted(IOperation operation)
    {
        while (operation is IConversionOperation { IsImplicit: true } conversion)
        {
            operation = conversion.Operand;
        }

        return operation;
    }

    // Where the value lands: an argument of a SqlArtisan member or an operand of a
    // SqlArtisan operator, reached through implicit conversions, an enclosing string
    // concatenation and a params array. Anything else (a local, a ternary) stops the walk.
    private static bool TryFindSqlArtisanSink(IOperation operation, out ITypeSymbol? parameterType)
    {
        parameterType = null;
        IOperation child = operation;
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
            child = current;
            current = current.Parent;
        }

        IMethodSymbol? method = current switch
        {
            IArgumentOperation { Parent: IInvocationOperation invocation } =>
                invocation.TargetMethod,
            IArgumentOperation { Parent: IObjectCreationOperation creation } =>
                creation.Constructor,
            IBinaryOperation binary => binary.OperatorMethod,
            IUnaryOperation unary => unary.OperatorMethod,
            _ => null,
        };
        if (method is null || !DialectUsageAnalyzer.IsFromSqlArtisan(method.ContainingAssembly))
        {
            return false;
        }

        parameterType = current switch
        {
            IArgumentOperation { Parameter: { IsParams: true, Type: IArrayTypeSymbol array } } =>
                array.ElementType,
            IArgumentOperation argument => argument.Parameter?.Type,
            IBinaryOperation binary when method.Parameters.Length == 2 =>
                method.Parameters[binary.LeftOperand == child ? 0 : 1].Type,
            _ => method.Parameters.FirstOrDefault()?.Type,
        };
        return true;
    }
}
