using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Reports SQLA0105 for a literal argument value <see cref="ArgumentValueValidity"/>
/// says the target dialect rejects — a <c>RegexpOptions</c> member outside that
/// engine's match-parameter alphabet (#528), or a negative row count on an engine
/// that refuses one (#529).
/// </summary>
/// <remarks>
/// Silent whenever a fact is missing (a non-constant argument, an engine absent
/// from the table) and, through <see cref="ValueDomainScope"/>, on a dialect
/// SQLA0100/SQLA0101 already flags — the same contract SQLA0104 follows.
/// </remarks>
internal static class ArgumentValueValidityRule
{
    /// <summary>The kind of value each message names, filling its <c>{2}</c>.</summary>
    private const string MatchOptionNoun = "match option";
    private const string RowCountNoun = "row count";

    public static void Check(
        OperationAnalysisContext context,
        IInvocationOperation invocation,
        DialectTargetSet targets)
    {
        string memberName = invocation.TargetMethod.Name;

        if (ArgumentValueValidity.MatchOptionParameterName.TryGetValue(
            memberName, out string? optionsParameter))
        {
            CheckMatchOptions(context, invocation, targets, memberName, optionsParameter);
        }

        if (ArgumentValueValidity.RowCountParameterName.TryGetValue(
            memberName, out string? countParameter))
        {
            CheckRowCount(context, invocation, targets, memberName, countParameter);
        }
    }

    private static void CheckMatchOptions(
        OperationAnalysisContext context,
        IInvocationOperation invocation,
        DialectTargetSet targets,
        string memberName,
        string parameterName)
    {
        if (FindArgument(invocation.Arguments, parameterName) is not { } argument
            || ResolveSetFlagNames(argument.Value) is not { Count: > 0 } setFlags
            || ValueDomainScope.For(context, invocation) is not { } scope)
        {
            return;
        }

        // One diagnostic per rejected member: the enum is [Flags], so a single
        // argument can carry more than one letter the target has no spelling for.
        foreach (string flag in setFlags)
        {
            List<string>? invalidOn = null;

            foreach (TargetDbms dbms in targets.Members)
            {
                if (ArgumentValueValidity.MatchOptionsFor(dbms) is { } valid
                    && !valid.Contains(flag)
                    && scope.Covers(dbms, targets))
                {
                    (invalidOn ??= []).Add(TargetDbmsNames.Display(dbms));
                }
            }

            Report(context, argument, memberName, flag, MatchOptionNoun, invalidOn);
        }
    }

    private static void CheckRowCount(
        OperationAnalysisContext context,
        IInvocationOperation invocation,
        DialectTargetSet targets,
        string memberName,
        string parameterName)
    {
        // Only a negative constant is a fact: zero runs (ADR 0012 records
        // `LIMIT 0` on two engines) and a count the analyzer cannot see stays
        // the database's business, as ADR 0004 has it.
        if (FindArgument(invocation.Arguments, parameterName) is not { } argument
            || argument.Value.ConstantValue is not { HasValue: true, Value: int count }
            || count >= 0
            || ValueDomainScope.For(context, invocation) is not { } scope)
        {
            return;
        }

        List<string>? invalidOn = null;

        foreach (TargetDbms dbms in targets.Members)
        {
            if (ArgumentValueValidity.RejectsNegativeRowCount(memberName, dbms)
                && scope.Covers(dbms, targets))
            {
                (invalidOn ??= []).Add(TargetDbmsNames.Display(dbms));
            }
        }

        Report(
            context,
            argument,
            memberName,
            count.ToString(CultureInfo.InvariantCulture),
            RowCountNoun,
            invalidOn);
    }

    private static void Report(
        OperationAnalysisContext context,
        IArgumentOperation argument,
        string memberName,
        string value,
        string noun,
        List<string>? invalidOn)
    {
        if (invalidOn is not { Count: > 0 })
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.InvalidArgumentValue,
            argument.Value.Syntax.GetLocation(),
            memberName,
            value,
            noun,
            TargetDbmsNames.JoinDisplayNames(invalidOn)));
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
    // technique): the member names come from the compilation and so do the bits
    // they are tested with, so the analyzer hardcodes neither. A zero-valued
    // member is skipped — every combination would "contain" it, and `None`
    // emits the empty match parameter every engine takes.
    private static List<string>? ResolveSetFlagNames(IOperation value)
    {
        if (value.ConstantValue is not { HasValue: true, Value: { } constant }
            || value.Type is not INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType
            || ToBits(constant) is not { } bits)
        {
            return null;
        }

        List<string> names = [];

        foreach (ISymbol member in enumType.GetMembers())
        {
            if (member is IFieldSymbol { HasConstantValue: true, ConstantValue: { } fieldValue }
                && ToBits(fieldValue) is { } fieldBits
                && fieldBits != 0
                && (bits & fieldBits) == fieldBits)
            {
                names.Add(member.Name);
            }
        }

        return names;
    }

    // Every enum underlying type spelled out rather than Convert.ToInt64: an
    // analyzer that throws takes the whole compilation's analysis down with it.
    private static long? ToBits(object value) => value switch
    {
        int i => i,
        uint u => u,
        long l => l,
        ulong u when u <= long.MaxValue => (long)u,
        short s => s,
        ushort u => u,
        sbyte s => s,
        byte b => b,
        _ => null,
    };
}
