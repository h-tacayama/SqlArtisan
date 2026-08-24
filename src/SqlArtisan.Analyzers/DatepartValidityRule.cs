using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Reports SQLA0104 for a literal <c>DateTimePart</c> argument this rule's
/// per-(member, dialect) table (<see cref="DatepartValidity"/>) says the target
/// dialect does not accept for that function — a finer grain than SQLA0100's
/// whole-construct verdict (#449).
/// </summary>
/// <remarks>
/// Silent, never a false positive, in three cases: the argument is not a
/// compile-time constant (a variable holding a computed <c>DateTimePart</c>);
/// its member name is not in <see cref="DatepartValidity"/>'s table for this
/// (member, dialect) pair (nothing to check); or the matrix already flags the
/// construct itself unsupported on that dialect (SQLA0100 owns that verdict —
/// reporting both would be redundant).
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

        // An `unsupported` construct override makes SQLA0100 fire for every
        // target, so the never-both-fire contract below covers the override
        // path too, not just the matrix's own verdict.
        AnalyzerConfigOptions options =
            context.Options.AnalyzerConfigOptionsProvider.GetOptions(invocation.Syntax.SyntaxTree);
        if (DialectSupportResolver.ResolveOverride(options, memberName, arity) is { IsSupported: false })
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

            // SQLA0100 already owns "this construct doesn't run on this dialect at
            // all" — skip a dialect the matrix has already flagged unsupported so
            // the two rules never both fire for the same usage.
            if (DialectSupportResolver.MatchMatrixEntry(memberName, arity) is { } match
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

    // Matches a literal DateTimePart argument (e.g. `DateTimePart.Epoch`) to its
    // member name via the constant's value and the argument's own enum type — the
    // same technique SchemaMetadata.Category uses to resolve a TypeCategory
    // attribute argument, so no build reference to the core's DateTimePart is
    // needed (ADR 0009).
    //
    // Deliberately does not unwrap a conversion: a cast or implicit constant
    // (`(DateTimePart)10`, `0`) is an IConversionOperation already typed
    // DateTimePart and already carrying the constant, so unwrapping to the
    // underlying int would lose the enum type and silently skip a value the
    // rule can resolve.
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
