using System.Diagnostics;
using static InlineSqlSharp.ExprResolver;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    public static ISelectBuilderSelect SELECT(
        params object[] selectItems) =>
        new SelectBuilder(SelectClause.Parse(selectItems));

    public static ISelectBuilderSelect SELECT(
        Distinct distinct,
        params object[] selectItems) =>
        new SelectBuilder(
            SelectClauseWithDistinct.Parse(
                distinct,
                selectItems));

    public static ISelectBuilderSelect SELECT(
        Hints hints,
        params object[] selectItems) =>
        new SelectBuilder(
            SelectClauseWithHints.Parse(
                hints,
                selectItems));

    public static ISelectBuilderSelect SELECT(
        Hints hints,
        Distinct distinct,
        params object[] selectItems) =>
        new SelectBuilder(
            SelectClauseWithOptions.Parse(
                hints,
                distinct,
                selectItems));

    public static Sequence SEQUENCE(string name) => new(name);

    public static SubstrFunction SUBSTR(
        object source,
        object position) => new(
            Resolve(source),
            Resolve(position));

    public static SubstrFunction SUBSTR(
        object source,
        object position,
        object length) => new(
            Resolve(source),
            Resolve(position),
            Resolve(length));

    public static SubstrbFunction SUBSTRB(
        object source,
        object position) => new(
            Resolve(source),
            Resolve(position));

    public static SubstrbFunction SUBSTRB(
        object source,
        object position,
        object length) => new(
            Resolve(source),
            Resolve(position),
            Resolve(length));

    public static SumFunction SUM(object expr) =>
        new(Resolve(expr));

    public static SumFunctionWithDistinct SUM(Distinct distinct, object expr) =>
        new(distinct, Resolve(expr));

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static SysdateFunction SYSDATE => new();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static SystimestampFunction SYSTIMESTAMP => new();
}
