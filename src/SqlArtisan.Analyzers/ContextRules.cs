using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Reports SQLA0003 for a construct the target dialect supports, but not in the
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

    // Climbs to the SELECT-list/HAVING/ORDER BY invocation hosting Grouping(); any
    // other argument host stops the climb rather than risk crossing into another query.
    private static IInvocationOperation? FindClauseAnchor(IInvocationOperation grouping)
    {
        IOperation current = grouping;
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
                    when DialectUsageAnalyzer.IsFromSqlArtisan(host.TargetMethod.ContainingAssembly)
                        && host.TargetMethod.Name is "Select" or "Having" or "OrderBy":
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
