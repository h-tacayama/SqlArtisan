using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        // The name-keyed tables match simple names, valid only for SqlArtisan's own
        // members — a user class sharing a key's name (TableClassGen names classes
        // after tables) must take the base-chain trace instead.
        if (DialectUsageAnalyzer.IsFromSqlArtisan(member.ContainingAssembly)
            && ResolveIdentifierParams(member) is { } identifierParams)
        {
            foreach (IdentifierParam identifier in identifierParams)
            {
                if (FindArgument(arguments, identifier.Name) is { } argument)
                {
                    CheckArgument(context, argument.Value, identifier.IsList, targets);
                }
            }

            return;
        }

        if (member.MethodKind == MethodKind.Constructor
            && FindInheritedIdentifierArgument(context.Compilation, member, arguments) is { } inherited)
        {
            CheckArgument(context, inherited, isList: false, targets);
        }
    }

    // Which base-constructor parameter carries the identifier a table class
    // forwards; keys are matched by simple name plus the SqlArtisan assembly.
    private static readonly Dictionary<string, string> InheritedIdentifierParams = new(StringComparer.Ordinal)
    {
        ["DbTableBase"] = "tableAlias",
        ["CteBase"] = "name",
        ["DerivedTableBase"] = "name",
    };

    // Matches CorrelatedDmlRule's chain bound; a deeper hierarchy fails toward silence.
    private const int CtorChainDepthLimit = 8;

    internal static bool DerivesFromIdentifierBase(ITypeSymbol? type)
    {
        for (ITypeSymbol? current = type?.BaseType; current is not null; current = current.BaseType)
        {
            if (IsIdentifierBase(current))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsIdentifierBase(ITypeSymbol type) =>
        InheritedIdentifierParams.ContainsKey(type.Name)
        && DialectUsageAnalyzer.IsFromSqlArtisan(type.ContainingAssembly);

    // A table class declares no identifier parameter of its own — it forwards a
    // constructor argument to DbTableBase's alias (or CteBase/DerivedTableBase's
    // name). Follow the ": base(...)" chain, propagating which creation-site
    // argument each parameter carries, until the naming base is reached; any
    // shape the walk cannot read fails toward silence.
    private static IOperation? FindInheritedIdentifierArgument(
        Compilation compilation,
        IMethodSymbol constructor,
        ImmutableArray<IArgumentOperation> arguments)
    {
        Dictionary<IParameterSymbol, IOperation> sources = new(SymbolEqualityComparer.Default);
        foreach (IArgumentOperation argument in arguments)
        {
            if (argument.Parameter is { } parameter && argument.ArgumentKind == ArgumentKind.Explicit)
            {
                sources[parameter] = argument.Value;
            }
        }

        for (int depth = 0; depth < CtorChainDepthLimit; depth++)
        {
            if (IsIdentifierBase(constructor.ContainingType))
            {
                string identifierName = InheritedIdentifierParams[constructor.ContainingType.Name];
                foreach (IParameterSymbol parameter in constructor.Parameters)
                {
                    if (parameter.Name == identifierName)
                    {
                        return sources.TryGetValue(parameter, out IOperation? source) ? source : null;
                    }
                }

                return null;
            }

            if (constructor.DeclaringSyntaxReferences.Length != 1
                || constructor.DeclaringSyntaxReferences[0].GetSyntax() is not ConstructorDeclarationSyntax declaration
                || declaration.Initializer is not { } initializer)
            {
                return null;
            }

            SemanticModel model = compilation.GetSemanticModel(initializer.SyntaxTree);
            if (model.GetOperation(initializer) is not IInvocationOperation call
                || call.TargetMethod.MethodKind != MethodKind.Constructor)
            {
                return null;
            }

            Dictionary<IParameterSymbol, IOperation> next = new(SymbolEqualityComparer.Default);
            foreach (IArgumentOperation argument in call.Arguments)
            {
                if (argument.Parameter is not { } parameter)
                {
                    continue;
                }

                IOperation value = argument.Value is IConversionOperation conversion
                    ? conversion.Operand
                    : argument.Value;
                if (value is IParameterReferenceOperation reference
                    && sources.TryGetValue(reference.Parameter, out IOperation? origin))
                {
                    next[parameter] = origin;
                }
            }

            sources = next;
            constructor = call.TargetMethod;
        }

        return null;
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
