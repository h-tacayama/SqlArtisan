using System.Diagnostics;
using static InlineSqlSharp.ExprResolver;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    public static ISelectBuilderSelect SELECT(
        params object[] selectItems) =>
        new SelectBuilder(
            SelectClause.Parse(
                null,
                null,
                selectItems));

    public static ISelectBuilderSelect SELECT(
        Distinct distinct,
        params object[] selectItems) =>
        new SelectBuilder(
            SelectClause.Parse(
                null,
                distinct,
                selectItems));

    public static ISelectBuilderSelect SELECT(
        Hints hints,
        params object[] selectItems) =>
        new SelectBuilder(
            SelectClause.Parse(
                hints,
                null,
                selectItems));

    public static ISelectBuilderSelect SELECT(
        Hints hints,
        Distinct distinct,
        params object[] selectItems) =>
        new SelectBuilder(
            SelectClause.Parse(
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
        new(null, Resolve(expr));

    public static SumFunction SUM(Distinct distinct, object expr) =>
        new(distinct, Resolve(expr));

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static SysdateFunction SYSDATE => new();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static SystimestampFunction SYSTIMESTAMP => new();
}
