using System.Diagnostics;
using static SqlArtisan.ExprResolver;

namespace SqlArtisan;

public static partial class SqlWordbook
{
    public static ISelectBuilderSelect Select(
        params object[] selectItems) =>
        new SelectBuilder(SelectClause.Parse(selectItems));

    public static ISelectBuilderSelect Select(
        DistinctKeyword distinct,
        params object[] selectItems) =>
        new SelectBuilder(
            SelectClauseWithDistinct.Parse(
                distinct,
                selectItems));

    public static ISelectBuilderSelect Select(
        SqlHints hints,
        params object[] selectItems) =>
        new SelectBuilder(
            SelectClauseWithHints.Parse(
                hints,
                selectItems));

    public static ISelectBuilderSelect Select(
        SqlHints hints,
        DistinctKeyword distinct,
        params object[] selectItems) =>
        new SelectBuilder(
            SelectClauseWithOptions.Parse(
                hints,
                distinct,
                selectItems));

    public static SequenceObject Sequence(string name) => new(name);

    public static SubstrFunction Substr(
        object source,
        object position) => new(
            Resolve(source),
            Resolve(position));

    public static SubstrFunction Substr(
        object source,
        object position,
        object length) => new(
            Resolve(source),
            Resolve(position),
            Resolve(length));

    public static SubstrBFunction SubstrB(
        object source,
        object position) => new(
            Resolve(source),
            Resolve(position));

    public static SubstrBFunction SubstrB(
        object source,
        object position,
        object length) => new(
            Resolve(source),
            Resolve(position),
            Resolve(length));

    public static SumFunction Sum(object expr) =>
        new(Resolve(expr));

    public static SumFunction Sum(DistinctKeyword distinct, object expr) =>
        new(distinct, Resolve(expr));

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static SysDateFunction SysDate => new();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static SysTimestampFunction SysTimestamp => new();
}
