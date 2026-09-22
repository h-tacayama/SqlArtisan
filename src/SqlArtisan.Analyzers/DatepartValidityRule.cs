using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Reports SQLA0104 for a literal <c>DateTimePart</c> argument
/// <see cref="DatepartValidity"/> says the target dialect rejects for that
/// function — a finer grain than SQLA0100's whole-construct verdict (#449).
/// </summary>
/// <remarks>
/// Silent whenever a fact is missing (a non-constant argument, a pair absent
/// from the table) and on a dialect the matrix already flags unsupported —
/// SQLA0100 owns that verdict; reporting both would be redundant.
/// </remarks>
internal static class DatepartValidityRule
{
    public static void Check(
        OperationAnalysisContext context,
        IInvocationOperation invocation,
        DialectTargetSet targets)
    {
        string memberName = invocation.TargetMethod.Name;
        if (!DatepartValidity.DatepartParameterName.TryGetValue(
                memberName, out string? parameterName)
            || FindArgument(invocation.Arguments, parameterName) is not { } argument
            || ResolveEnumMemberName(argument.Value) is not { } datepart)
        {
            return;
        }

        // A null scope means an `unsupported` override has already handed every
        // target to SQLA0100.
        if (ValueDomainScope.For(context, invocation) is not { } scope)
        {
            return;
        }

        List<string>? invalidOn = null;

        foreach (TargetDbms dbms in targets.Members)
        {
            if (DatepartValidity.For(memberName, dbms) is { } valid
                && !valid.Contains(datepart)
                && scope.Covers(dbms, targets))
            {
                (invalidOn ??= []).Add(TargetDbmsNames.Display(dbms));
            }
        }

        if (invalidOn is { Count: > 0 })
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.InvalidDatepartArgument,
                argument.Value.Syntax.GetLocation(),
                memberName,
                datepart,
                TargetDbmsNames.JoinDisplayNames(invalidOn)));
        }
    }

    private static IArgumentOperation? FindArgument(
        ImmutableArray<IArgumentOperation> arguments, string parameterName)
    {
        foreach (IArgumentOperation argument in arguments)
        {
            if (argument.Parameter?.Name == parameterName)
            {
                return argument;
            }
        }

        return null;
    }

    // Resolved against the argument's own enum type (ADR 0009's no-core-reference
    // technique, as SchemaMetadata.Category). No conversion unwrap: a cast like
    // `(DateTimePart)10` already carries the typed constant — unwrapping loses it.
    private static string? ResolveEnumMemberName(IOperation value)
    {
        if (value.ConstantValue is not { HasValue: true } constant
            || value.Type is not INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType)
        {
            return null;
        }

        return enumType.GetMembers()
            .OfType<IFieldSymbol>()
            .FirstOrDefault(f => f.HasConstantValue && Equals(f.ConstantValue, constant.Value))
            ?.Name;
    }
}
