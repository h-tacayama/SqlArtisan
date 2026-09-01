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
        if (!DatepartValidity.DatepartParameterName.TryGetValue(memberName, out string? parameterName)
            || FindArgument(invocation.Arguments, parameterName) is not { } argument
            || ResolveEnumMemberName(argument.Value) is not { } datepart)
        {
            return;
        }

        int? arity = invocation.TargetMethod.Parameters.Length;

        // An `unsupported` override makes SQLA0100 fire for every target, so the
        // never-both-fire contract below must cover the override path too.
        AnalyzerConfigOptions options =
            context.Options.AnalyzerConfigOptionsProvider.GetOptions(invocation.Syntax.SyntaxTree);
        DialectSupportResolver.OverrideResult? overrideResult =
            DialectSupportResolver.ResolveOverride(options, memberName, arity);
        if (overrideResult is { IsSupported: false })
        {
            return;
        }

        List<string>? invalidOn = null;

        foreach (TargetDbms dbms in targets.Members)
        {
            if (DatepartValidity.For(memberName, dbms) is not { } valid)
            {
                continue;
            }

            // Skip dialects SQLA0100/0101 already flag — unless a `supported` override
            // silenced them: the user asserts it runs there, so this check re-arms.
            if (overrideResult is not { IsSupported: true }
                && DialectSupportResolver.MatchMatrixEntry(memberName, arity) is { } match
                && !DialectSupportResolver.Evaluate(match, dbms, targets.VersionFor(dbms)).IsSupported)
            {
                continue;
            }

            if (!valid.Contains(datepart))
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
