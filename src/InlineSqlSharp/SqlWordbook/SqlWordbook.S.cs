using System.Diagnostics;
using static InlineSqlSharp.ExprResolver;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    public static ISelectBuilderSelect SELECT(
        params object[] selectItems) =>
        new SelectBuilder(
            SelectClause.Parse(
                Hints.None,
                AllOrDistinct.All,
                selectItems));

    public static ISelectBuilderSelect SELECT(
        AllOrDistinct allOrAistinct,
        params object[] selectItems) =>
        new SelectBuilder(
            SelectClause.Parse(
                Hints.None,
                allOrAistinct,
                selectItems));

    public static ISelectBuilderSelect SELECT(
        Hints hints,
        params object[] selectItems) =>
        new SelectBuilder(
            SelectClause.Parse(
                hints,
                AllOrDistinct.All,
                selectItems));

    public static ISelectBuilderSelect SELECT(
        Hints hints,
        AllOrDistinct allOrAistinct,
        params object[] selectItems) =>
        new SelectBuilder(
            SelectClause.Parse(
                hints,
                allOrAistinct,
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
        new(AllOrDistinct.All, Resolve(expr));

    public static SumFunction SUM(AllOrDistinct allOrDistinct, object expr) =>
        new(allOrDistinct, Resolve(expr));

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static SysdateFunction SYSDATE => new();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static SystimestampFunction SYSTIMESTAMP => new();
}
