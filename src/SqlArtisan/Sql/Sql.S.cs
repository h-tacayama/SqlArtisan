using System.Diagnostics;
using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
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

    /// <summary>
    /// Creates a reference to a sequence using the Oracle dotted syntax,
    /// e.g. <c>name.NEXTVAL</c> / <c>name.CURRVAL</c> via <c>.Nextval</c> / <c>.Currval</c>.
    /// </summary>
    /// <remarks>
    /// Dialect-specific (Oracle). For PostgreSQL use <see cref="Nextval(string)"/> /
    /// <see cref="Currval(string)"/>; for SQL Server use <see cref="NextValueFor(string)"/>.
    /// </remarks>
    public static DbSequence Sequence(string name) => new(name);

    /// <summary>
    /// The <c>SIGN(expr)</c> function: the sign of <paramref name="expr"/>
    /// (-1, 0, or 1).
    /// </summary>
    public static SignFunction Sign(object expr) =>
        new(Resolve(expr));

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static SkipLockedBehavior SkipLocked => new();

    /// <summary>
    /// The <c>SQRT(expr)</c> function: the square root of <paramref name="expr"/>.
    /// </summary>
    public static SqrtFunction Sqrt(object expr) =>
        new(Resolve(expr));

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

    public static SubstrbFunction Substrb(
        object source,
        object position) => new(
            Resolve(source),
            Resolve(position));

    public static SubstrbFunction Substrb(
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

    /// <summary>
    /// The <c>SYSDATE</c> function. Dialect-specific (Oracle). For the standard
    /// current date/time use <see cref="CurrentTimestamp"/> or <see cref="CurrentDate"/>.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static SysdateFunction Sysdate => new();

    /// <summary>
    /// The <c>SYSTIMESTAMP</c> function. Dialect-specific (Oracle). For the standard
    /// current timestamp use <see cref="CurrentTimestamp"/>.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public static SystimestampFunction Systimestamp => new();
}
