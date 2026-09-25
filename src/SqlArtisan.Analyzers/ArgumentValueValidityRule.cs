using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Reports SQLA0104 for a literal argument value <see cref="ArgumentValueValidity"/> says the
/// target dialect rejects: a <c>RegexpOptions</c> member outside its match-parameter alphabet
/// (#528), a negative row count (#529), or a <c>GROUP BY</c> position or non-integer key (#537).
/// </summary>
/// <remarks>
/// The sibling of <see cref="DatepartValidityRule"/> under the same id — same
/// verdict, same remediation — silent whenever a fact is missing, and, through
/// <see cref="ValueDomainScope"/>, on a dialect SQLA0100/SQLA0101 already flags.
/// </remarks>
internal static class ArgumentValueValidityRule
{
    // The kind of value a message names, filling its {2}.
    private const string MatchOptionNoun = "match option";
    private const string RowCountNoun = "row count";
    private const string OrdinalNoun = "column ordinal";
    private const string GroupKeyNoun = "group key";

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

        if (memberName == "GroupBy")
        {
            CheckGroupByOrdinal(context, invocation, targets, memberName);
        }
    }

    private static void CheckGroupByOrdinal(
        OperationAnalysisContext context,
        IInvocationOperation invocation,
        DialectTargetSet targets,
        string memberName)
    {
        if (FindArgument(invocation.Arguments, ArgumentValueValidity.GroupByItemsParameterName)
                is not { } argument
            || ValueDomainScope.For(context, invocation) is not { } scope)
        {
            return;
        }

        List<string>? invalidOn = null;
        List<string>? fractionalInvalidOn = null;

        foreach (TargetDbms dbms in targets.Members)
        {
            if (!scope.Covers(dbms, targets))
            {
                continue;
            }

            if (ArgumentValueValidity.RejectsOrdinalGroupBy(dbms))
            {
                (invalidOn ??= []).Add(TargetDbmsNames.Display(dbms));
            }

            if (ArgumentValueValidity.RejectsFractionalGroupKey(dbms))
            {
                (fractionalInvalidOn ??= []).Add(TargetDbmsNames.Display(dbms));
            }
        }

        if (invalidOn is not { Count: > 0 } && fractionalInvalidOn is not { Count: > 0 })
        {
            return;
        }

        // Per element, so a mixed list reports only the keys in it.
        foreach (IOperation element in Elements(Unwrap(argument.Value)))
        {
            IOperation key = Unwrap(element);
            if (key.ConstantValue is not { HasValue: true, Value: { } value })
            {
                continue;
            }

            // The library renders every integral type as an ordinal, so the
            // check follows the type rather than the boxed int an enum carries.
            if (IsOrdinalType(key.Type) is true)
            {
                // Below 1 is rejected at the call on every dialect, so naming
                // one here would point at the wrong problem.
                if (ToOrdinal(value) is > 0 && invalidOn is { Count: > 0 })
                {
                    Report(context, element, memberName, value, OrdinalNoun, invalidOn);
                }
            }
            else if (IsFractionalType(key.Type) is true && fractionalInvalidOn is { Count: > 0 })
            {
                Report(context, element, memberName, value, GroupKeyNoun, fractionalInvalidOn);
            }
        }
    }

    private static void Report(
        OperationAnalysisContext context,
        IOperation element,
        string memberName,
        object value,
        string noun,
        List<string> invalidOn) =>
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.InvalidArgumentValue,
            element.Syntax.GetLocation(),
            memberName,
            Convert.ToString(value, CultureInfo.InvariantCulture),
            noun,
            TargetDbmsNames.JoinDisplayNames(invalidOn)));

    private static bool IsOrdinalType(ITypeSymbol? type) => type?.SpecialType is
        SpecialType.System_SByte or SpecialType.System_Byte or SpecialType.System_Int16
        or SpecialType.System_UInt16 or SpecialType.System_Int32 or SpecialType.System_UInt32
        or SpecialType.System_Int64 or SpecialType.System_UInt64
        or SpecialType.System_IntPtr or SpecialType.System_UIntPtr;

    private static bool IsFractionalType(ITypeSymbol? type) => type?.SpecialType is
        SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal;

    private static long? ToOrdinal(object value) => value switch
    {
        sbyte v => v,
        byte v => v,
        short v => v,
        ushort v => v,
        int v => v,
        uint v => v,
        long v => v,
        ulong v when v <= long.MaxValue => (long)v,
        nint v => v,
        _ => null,
    };

    // A params array's elements. Unlike IdentifierLengthRule's copy this must
    // also refuse an array with no initializer, whose only child is its length.
    private static IEnumerable<IOperation> Elements(IOperation value) =>
        value switch
        {
            IArrayCreationOperation { Initializer: { } initializer } => initializer.ElementValues,
            IArrayCreationOperation => [],
            { Type: IArrayTypeSymbol } and not (IInvocationOperation or ILocalReferenceOperation
                or IParameterReferenceOperation or IFieldReferenceOperation
                or IPropertyReferenceOperation or IConversionOperation) => value.ChildOperations,
            _ => [],
        };

    // A value reaches `object` through a boxing conversion, which carries the
    // constant; the key is the operand's, not the conversion's. The params
    // argument itself is wrapped the same way when written as `[1, 2]`.
    private static IOperation Unwrap(IOperation element) =>
        element is IConversionOperation conversion ? conversion.Operand : element;

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
        // Only a negative constant is a fact: `LIMIT 0` runs, and a count the
        // analyzer cannot see stays the database's business (ADR 0004).
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
    // technique): both the member names and the bits they are tested with come
    // from the compilation. A zero-valued member would match every combination.
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
