using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Reports SQLA0102 for a construct the target dialect supports, but not in the
/// syntactic position it sits in — verdicts a per-construct matrix entry cannot
/// express (#264).
/// </summary>
/// <remarks>
/// Every walk is a whitelist over parent-operation shapes: an unrecognized shape
/// returns silently, so indirection (a builder held in a variable, a helper
/// method) yields a false negative, never a false positive (ADR 0003).
/// </remarks>
internal static class ContextRules
{
    /// <summary>
    /// MySQL rejects <c>LIMIT</c> directly inside an <c>IN</c>/<c>ANY</c>/<c>ALL</c>/<c>SOME</c>
    /// subquery (ER_NOT_SUPPORTED_YET), though scalar, <c>EXISTS</c>, CTE, and
    /// derived-table positions accept it.
    /// </summary>
    public static void CheckLimitInQuantifiedSubquery(
        OperationAnalysisContext context, IInvocationOperation limit, string dialectName)
    {
        IOperation current = limit;
        while (FluentChain.Parent(current) is { } link)
        {
            current = link;
        }

        IOperation? parent = FluentChain.SkipConversion(current).Parent;
        if (parent is not IArgumentOperation argument || !IsQuantifiedSubqueryArgument(argument))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ContextRestrictedConstruct,
            limit.Syntax.GetLocation(),
            "Limit",
            "inside an IN/ANY/ALL/SOME subquery",
            dialectName));
    }

    /// <summary>
    /// Absence is provable at the type level: <c>WithRollup()</c> is declared only
    /// on the stage <c>GroupBy(...)</c> returns, so a chain whose call after
    /// <c>GroupBy</c> is anything else can never acquire it.
    /// </summary>
    public static void CheckGroupingRequiresWithRollup(
        OperationAnalysisContext context, IInvocationOperation grouping, string dialectName)
    {
        if (FindClauseAnchor(grouping) is not { } anchor)
        {
            return;
        }

        List<string> names = VisibleChainNames(anchor);
        int groupByIndex = names.IndexOf("GroupBy");
        if (groupByIndex < 0
            || groupByIndex == names.Count - 1
            || names[groupByIndex + 1] == "WithRollup")
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ContextRestrictedConstruct,
            grouping.Syntax.GetLocation(),
            "Grouping",
            "outside a WITH ROLLUP query",
            dialectName));
    }

    /// <summary>
    /// SQL Server exposes the percentiles only as window functions, so the bare
    /// <c>WITHIN GROUP</c> form Oracle and PostgreSQL accept has no spelling there.
    /// </summary>
    public static void CheckPercentileRequiresOver(
        OperationAnalysisContext context, IInvocationOperation percentile, string dialectName)
    {
        if (FindArgumentHost(percentile) is null)
        {
            return;
        }

        bool withinGroup = false;
        bool over = false;
        IOperation current = percentile;
        while (FluentChain.Parent(current) is { } link)
        {
            withinGroup |= link.TargetMethod.Name == "WithinGroup";
            over |= link.TargetMethod.Name == "Over";
            current = link;
        }

        if (!withinGroup || over)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ContextRestrictedConstruct,
            percentile.Syntax.GetLocation(),
            percentile.TargetMethod.Name,
            "outside an OVER clause",
            dialectName));
    }

    /// <summary>
    /// The <c>INSERTED</c>/<c>DELETED</c> pseudo-tables name the row images an
    /// <c>OUTPUT</c> clause reads; elsewhere the reference binds to no table.
    /// </summary>
    public static void CheckPseudoTableRequiresOutput(
        OperationAnalysisContext context, IInvocationOperation pseudoTable, string dialectName)
    {
        if (FindArgumentHost(pseudoTable) is not { } host)
        {
            return;
        }

        // Every enclosing host counts, not just the innermost: the clause binds the
        // pseudo-table through a wrapping function too (OUTPUT COALESCE(INSERTED.c, 0)).
        for (IInvocationOperation? cursor = host; cursor is not null; cursor = FindArgumentHost(cursor))
        {
            if (cursor.TargetMethod.Name == "Output")
            {
                return;
            }
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ContextRestrictedConstruct,
            pseudoTable.Syntax.GetLocation(),
            pseudoTable.TargetMethod.Name,
            "outside an OUTPUT clause",
            dialectName));
    }

    /// <summary>
    /// MySQL's <c>INTERVAL</c> keyword has no standalone value — it parses only
    /// as an immediate operand of <c>+</c>/<c>-</c> date arithmetic or as the
    /// <c>interval</c> argument of <c>DateAdd</c>/<c>DateSub</c> (never their
    /// <c>date</c> argument), the same restriction whether the spelling came
    /// from <c>Interval</c> or the MySQL-accepted <c>IntervalLiteral</c>
    /// arity-2 form. Only the bare-argument shape is provable here — the climb
    /// stops silently at a qualifying operator/call (correct) or at anything
    /// else, including a variable a later one might still use (ADR 0003:
    /// false negative, never a false positive).
    /// </summary>
    public static void CheckIntervalRequiresArithmeticOperand(
        OperationAnalysisContext context, IInvocationOperation interval, string dialectName)
    {
        IOperation current = interval;
        while (true)
        {
            IOperation? parent = current.Parent;
            switch (parent)
            {
                case IBinaryOperation { OperatorMethod: { Name: "op_Addition" or "op_Subtraction" } method }
                    when DialectUsageAnalyzer.IsFromSqlArtisan(method.ContainingAssembly):
                    return;
                case IConversionOperation:
                case IArrayCreationOperation:
                case IArrayInitializerOperation:
                    current = parent;
                    break;
                case IArgumentOperation
                {
                    Parameter.Name: "interval",
                    Parent: IInvocationOperation { TargetMethod.Name: "DateAdd" or "DateSub" } host,
                }
                    when DialectUsageAnalyzer.IsFromSqlArtisan(host.TargetMethod.ContainingAssembly):
                    return;
                case IArgumentOperation { Parent: IInvocationOperation host }
                    when DialectUsageAnalyzer.IsFromSqlArtisan(host.TargetMethod.ContainingAssembly):
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.ContextRestrictedConstruct,
                        interval.Syntax.GetLocation(),
                        interval.TargetMethod.Name,
                        "outside a +/- date-arithmetic expression or a DATE_ADD/DATE_SUB call",
                        dialectName));
                    return;
                default:
                    return;
            }
        }
    }

    // Climbs to the SELECT-list/HAVING/ORDER BY invocation hosting Grouping(); any
    // other argument host stops the climb rather than risk crossing into another query.
    private static IInvocationOperation? FindClauseAnchor(IInvocationOperation grouping) =>
        FindArgumentHost(grouping) is { } host
            && host.TargetMethod.Name is "Select" or "Having" or "OrderBy"
                ? host
                : null;

    // Climbs to the invocation the expression is an argument of. Reaching one proves
    // the expression is consumed where it is written, so the chain between the two is
    // the whole chain — a receiver parked in a variable stops the climb instead.
    private static IInvocationOperation? FindArgumentHost(IInvocationOperation start)
    {
        IOperation current = start;
        while (true)
        {
            IOperation? parent = current.Parent;
            switch (parent)
            {
                case IConversionOperation:
                case IArrayCreationOperation:
                case IArrayInitializerOperation:
                case IBinaryOperation:
                    current = parent;
                    break;
                case IInvocationOperation chain
                    when chain.Instance == current
                        && DialectUsageAnalyzer.IsFromSqlArtisan(chain.TargetMethod.ContainingAssembly):
                    current = parent;
                    break;
                case IArgumentOperation { Parent: IInvocationOperation host }
                    when DialectUsageAnalyzer.IsFromSqlArtisan(
                        host.TargetMethod.ContainingAssembly):
                    return host;
                default:
                    return null;
            }
        }
    }

    // The fluent chain visible in the anchor's own expression, head to tail;
    // links reached through a variable or helper are invisible by design.
    private static List<string> VisibleChainNames(IInvocationOperation anchor)
    {
        var names = new List<string>();
        for (IInvocationOperation? cursor = anchor; cursor is not null; cursor = ChainChild(cursor))
        {
            names.Add(cursor.TargetMethod.Name);
        }

        names.Reverse();

        IOperation current = anchor;
        while (FluentChain.Parent(current) is { } link)
        {
            names.Add(link.TargetMethod.Name);
            current = link;
        }

        return names;
    }

    private static IInvocationOperation? ChainChild(IInvocationOperation invocation)
    {
        IOperation? instance = invocation.Instance;
        if (instance is IConversionOperation conversion)
        {
            instance = conversion.Operand;
        }

        return instance is IInvocationOperation child
            && DialectUsageAnalyzer.IsFromSqlArtisan(child.TargetMethod.ContainingAssembly)
                ? child
                : null;
    }

    // Parameter.Type identifies the overload that actually bound — the ISubquery
    // membership/quantified forms, never the params/collection value-list forms.
    private static bool IsQuantifiedSubqueryArgument(IArgumentOperation argument) =>
        argument.Parameter is { Type: { Name: "ISubquery" } parameterType }
        && DialectUsageAnalyzer.IsFromSqlArtisan(parameterType.ContainingAssembly)
        && argument.Parent is IInvocationOperation host
        && host.TargetMethod.Name is "In" or "NotIn" or "Any" or "All" or "Some"
        && DialectUsageAnalyzer.IsFromSqlArtisan(host.TargetMethod.ContainingAssembly);
}
