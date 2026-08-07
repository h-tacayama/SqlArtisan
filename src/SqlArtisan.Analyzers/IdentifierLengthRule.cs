using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Reports SQLA0103 for a compile-time identifier literal — a SELECT/table alias,
/// a CTE or derived-table name, a <c>VALUES</c> column name, or the Oracle
/// <c>RETURNING</c> output variable — that exceeds the target dialect's limit.
/// </summary>
/// <remarks>
/// Only identifiers the user mints in the query are covered; existing-schema names
/// already exist within the engine's limit. Arguments match by parameter name, so
/// overloads (e.g. <c>As(DbColumn)</c>) disambiguate without a core-type reference.
/// </remarks>
internal static class IdentifierLengthRule
{
    private static readonly Dictionary<string, IdentifierParam[]> MethodIdentifierParams = new(StringComparer.Ordinal)
    {
        ["As"] = [new IdentifierParam("alias", isList: false)],
        ["AsTable"] = [new IdentifierParam("alias", isList: false), new IdentifierParam("columns", isList: true)],
        ["Values"] = [new IdentifierParam("alias", isList: false), new IdentifierParam("columnNames", isList: true)],
    };

    private static readonly Dictionary<string, IdentifierParam[]> ConstructorIdentifierParams = new(StringComparer.Ordinal)
    {
        ["Cte"] = [new IdentifierParam("name", isList: false)],
        ["CteBase"] = [new IdentifierParam("name", isList: false)],
        ["DerivedTable"] = [new IdentifierParam("name", isList: false)],
        ["DerivedTableBase"] = [new IdentifierParam("name", isList: false)],
        ["DbTable"] = [new IdentifierParam("tableAlias", isList: false)],
        ["DbTableBase"] = [new IdentifierParam("tableAlias", isList: false)],
        ["OutputParameter"] = [new IdentifierParam("variable", isList: false)],
    };

    public static void Check(
        OperationAnalysisContext context,
        IMethodSymbol member,
        ImmutableArray<IArgumentOperation> arguments,
        DialectTargetSet targets)
    {
        if (ResolveIdentifierParams(member) is not { } identifierParams)
        {
            return;
        }

        foreach (IdentifierParam identifier in identifierParams)
        {
            if (FindArgument(arguments, identifier.Name) is { } argument)
            {
                CheckArgument(context, argument.Value, identifier.IsList, targets);
            }
        }
    }

    private static IdentifierParam[]? ResolveIdentifierParams(IMethodSymbol member) =>
        member.MethodKind == MethodKind.Constructor
            ? Lookup(ConstructorIdentifierParams, member.ContainingType?.Name)
            : Lookup(MethodIdentifierParams, member.Name);

    private static IdentifierParam[]? Lookup(Dictionary<string, IdentifierParam[]> table, string? key) =>
        key is not null && table.TryGetValue(key, out IdentifierParam[]? value) ? value : null;

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

    private static void CheckArgument(
        OperationAnalysisContext context, IOperation value, bool isList, DialectTargetSet targets)
    {
        IOperation unwrapped = value is IConversionOperation conversion ? conversion.Operand : value;

        if (!isList)
        {
            Report(context, unwrapped, targets);
            return;
        }

        // A string[] identifier list (VALUES column names), written as new[]{...} or [...]:
        // report per element so each over-long name gets its own location. Elements are
        // read via child operations rather than a collection-expression type, which the
        // pinned Roslyn version does not expose.
        foreach (IOperation element in Elements(unwrapped))
        {
            Report(context, element, targets);
        }
    }

    private static IEnumerable<IOperation> Elements(IOperation value) =>
        value is IArrayCreationOperation { Initializer: { } initializer }
            ? initializer.ElementValues
            : value.ChildOperations;

    // One diagnostic per DBMS in the set whose limit the identifier exceeds
    // (#432) — the limit and its unit are per-dialect, so unlike SQLA0100 the
    // failing dialects cannot join into one message.
    private static void Report(OperationAnalysisContext context, IOperation value, DialectTargetSet targets)
    {
        if (value.ConstantValue is not { HasValue: true, Value: string identifier })
        {
            return;
        }

        foreach (TargetDbms dbms in targets.Members)
        {
            if (IdentifierLengthLimits.For(dbms) is not { } limit
                || IdentifierLengthLimits.Measure(identifier, limit.Unit) <= limit.Limit)
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.IdentifierTooLong,
                value.Syntax.GetLocation(),
                identifier,
                TargetDbmsNames.Display(dbms),
                limit.Limit,
                limit.Unit == LengthUnit.Bytes ? "bytes" : "characters"));
        }
    }

    private readonly struct IdentifierParam
    {
        public IdentifierParam(string name, bool isList)
        {
            Name = name;
            IsList = isList;
        }

        public string Name { get; }

        public bool IsList { get; }
    }
}
