using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Reports SQLA0304: a <c>nameof</c> alias in the root select list of a query read by a typed
/// <c>SqlArtisan.Dapper</c> verb names no member of the model, so Dapper leaves one at its
/// default (#716, ADR 0029).
/// </summary>
internal static class DapperAliasRule
{
    internal const string DapperAssemblyName = "SqlArtisan.Dapper";

    // Internal for the parity gate: a typed verb SqlArtisan.Dapper adds must be placed in
    // one of these two sets deliberately.
    internal static readonly HashSet<string> CheckedVerbs =
    [
        "Query", "QueryAsync", "QueryFirst", "QueryFirstAsync", "QueryFirstOrDefault",
        "QueryFirstOrDefaultAsync", "QuerySingle", "QuerySingleAsync", "QuerySingleOrDefault",
        "QuerySingleOrDefaultAsync"
    ];

    internal static readonly HashSet<string> ExcludedVerbs =
    [
        "ExecuteScalar", "ExecuteScalarAsync"
    ];

    // Bounds the walk through locals and builder steps, so a pathological chain costs
    // a fixed amount and then fails toward silence.
    private const int StepLimit = 64;

    public static bool IsCheckedVerb(IMethodSymbol method) =>
        method.TypeArguments.Length == 1
        && CheckedVerbs.Contains(method.Name)
        && method.ContainingType?.ToDisplayString() == "SqlArtisan.Dapper.SqlMapper"
        && method.ContainingAssembly?.Name == DapperAssemblyName;

    public static void Check(OperationAnalysisContext context, IInvocationOperation call)
    {
        if (!IsCheckedVerb(call.TargetMethod)
            || call.Arguments.FirstOrDefault(a => a.Parameter?.Name == "sqlBuilder")
                is not { } builder
            || call.TargetMethod.TypeArguments[0] is not INamedTypeSymbol model
            || IsSilentModel(model, context.Compilation))
        {
            return;
        }

        IOperation body = Root(call);
        if (FindRootSelect(builder.Value, body) is not { } select)
        {
            return;
        }

        HashSet<string>? names = null;
        foreach ((IOperation nameOf, string alias) in NameOfAliases(select))
        {
            names ??= MemberNames(model);
            if (!names.Contains(Normalize(alias)))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.DapperAliasNamesNoMember,
                    nameOf.Syntax.GetLocation(),
                    alias,
                    model.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            }
        }
    }

    // A type declared in another assembly is silent whatever it is: `dotnet build` reads it
    // from metadata without its private members, which the IDE sees and Dapper fills.
    private static bool IsSilentModel(INamedTypeSymbol model, Compilation compilation)
    {
        if (model.IsAbstract
            || model.TypeKind is TypeKind.Interface or TypeKind.Enum
            || compilation.ReferencedAssemblyNames.Any(a => a.Name == "Dapper.FluentMap"))
        {
            return true;
        }

        for (INamedTypeSymbol? type = model; type is not null; type = type.BaseType)
        {
            if (!ReferenceEquals(type, model)
                && type.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType)
            {
                return false;
            }

            if (!IsDeclaredIn(type, compilation) || HasColumnAttribute(type))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDeclaredIn(INamedTypeSymbol type, Compilation compilation) =>
        SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, compilation.Assembly);

    private static bool HasColumnAttribute(INamedTypeSymbol type) =>
        IsColumnAttributed(type)
        || type.GetMembers().Any(m => IsColumnAttributed(m)
            || m is IMethodSymbol method && method.Parameters.Any(IsColumnAttributed));

    private static bool IsColumnAttributed(ISymbol symbol) =>
        symbol.GetAttributes().Any(a => a.AttributeClass?.Name == "ColumnAttribute");

    private static HashSet<string> MemberNames(INamedTypeSymbol model)
    {
        HashSet<string> names = [];
        for (INamedTypeSymbol? type = model; type is not null; type = type.BaseType)
        {
            foreach (ISymbol member in type.GetMembers())
            {
                if (member is IPropertySymbol or IFieldSymbol)
                {
                    names.Add(Normalize(member.Name));
                }
            }
        }

        foreach (IMethodSymbol constructor in model.InstanceConstructors)
        {
            foreach (IParameterSymbol parameter in constructor.Parameters)
            {
                names.Add(Normalize(parameter.Name));
            }
        }

        return names;
    }

    // Wider than Dapper's own matching, which ignores underscores only when
    // MatchNamesWithUnderscores is set at run time: any name it could match is silence.
    private static string Normalize(string name) =>
        name.Replace("_", string.Empty).ToUpperInvariant();

    private static IOperation Root(IOperation operation)
    {
        IOperation current = operation;
        while (current.Parent is { } parent)
        {
            current = parent;
        }

        return current;
    }

    private static IInvocationOperation? FindRootSelect(IOperation start, IOperation body)
    {
        IOperation? current = Follow(start, body);
        for (int steps = 0; steps < StepLimit && current is IInvocationOperation step; steps++)
        {
            IMethodSymbol method = step.TargetMethod;
            if (!DialectUsageAnalyzer.IsFromSqlArtisan(method.ContainingAssembly))
            {
                return null;
            }

            if (step.Instance is null)
            {
                return method.Name == "Select" && IsSqlFactory(method) ? step : null;
            }

            IOperation? receiver = Follow(step.Instance, body);
            if (method.Name == "Select"
                && receiver is IInvocationOperation with
                && with.TargetMethod.Name is "With" or "WithRecursive"
                && IsSqlFactory(with.TargetMethod))
            {
                return step;
            }

            current = receiver;
        }

        // Only method calls are walked through, so a set operator, a property, ends the walk:
        // which branch names the result columns is each engine's, and it is not verified.
        return null;
    }

    private static bool IsSqlFactory(IMethodSymbol method) =>
        method.ContainingType?.ToDisplayString() == "SqlArtisan.Sql"
        && DialectUsageAnalyzer.IsFromSqlArtisan(method.ContainingAssembly);

    // Through conversions, and through a local declared with an initializer and
    // written nowhere else in the member: the root's select list is fixed once Select runs.
    private static IOperation? Follow(IOperation operation, IOperation body)
    {
        IOperation current = operation;
        for (int steps = 0; steps < StepLimit; steps++)
        {
            while (current is IConversionOperation conversion)
            {
                current = conversion.Operand;
            }

            if (current is not ILocalReferenceOperation reference)
            {
                return current;
            }

            if (InitializerOf(reference.Local, body) is not { } initializer
                || IsWritten(reference.Local, body))
            {
                return null;
            }

            current = initializer;
        }

        return null;
    }

    private static IOperation? InitializerOf(ILocalSymbol local, IOperation body) =>
        body.DescendantsAndSelf()
            .OfType<IVariableDeclaratorOperation>()
            .FirstOrDefault(d => SymbolEqualityComparer.Default.Equals(d.Symbol, local))
            ?.GetVariableInitializer()?.Value;

    private static bool IsWritten(ILocalSymbol local, IOperation body) =>
        body.DescendantsAndSelf()
            .OfType<ILocalReferenceOperation>()
            .Any(r => SymbolEqualityComparer.Default.Equals(r.Local, local) && IsWrite(r));

    private static bool IsWrite(ILocalReferenceOperation reference)
    {
        IOperation child = reference;
        IOperation? parent = reference.Parent;
        while (parent is ITupleOperation)
        {
            child = parent;
            parent = parent.Parent;
        }

        return parent switch
        {
            IAssignmentOperation assignment => ReferenceEquals(assignment.Target, child)
                || assignment is ISimpleAssignmentOperation { IsRef: true },
            IIncrementOrDecrementOperation or IAddressOfOperation => true,
            IArgumentOperation argument => argument.Parameter?.RefKind is not RefKind.None,
            IVariableInitializerOperation { Parent: IVariableDeclaratorOperation declarator } =>
                declarator.Symbol.RefKind is not RefKind.None,
            _ => false,
        };
    }

    private static IEnumerable<(IOperation NameOf, string Alias)> NameOfAliases(
        IInvocationOperation select)
    {
        IArgumentOperation? items = select.Arguments.FirstOrDefault(
            a => a.ArgumentKind == ArgumentKind.ParamArray);
        if (items?.Value is not IArrayCreationOperation { Initializer: { } initializer })
        {
            yield break;
        }

        foreach (IOperation element in initializer.ElementValues)
        {
            IOperation item = element is IConversionOperation conversion
                ? conversion.Operand
                : element;
            if (item is IInvocationOperation { Instance: not null } alias
                && alias.TargetMethod.Name == "As"
                && alias.TargetMethod.Parameters.Length == 1
                && alias.TargetMethod.Parameters[0].Type.SpecialType == SpecialType.System_String
                && DialectUsageAnalyzer.IsFromSqlArtisan(alias.TargetMethod.ContainingAssembly)
                && alias.Arguments[0].Value is INameOfOperation nameOf
                && nameOf.Argument is IMemberReferenceOperation
                && nameOf.ConstantValue is { HasValue: true, Value: string name })
            {
                yield return (nameOf, name);
            }
        }
    }
}
