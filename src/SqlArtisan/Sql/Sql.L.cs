using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// The <c>LAG(expr)</c> analytic function: the value of
    /// <paramref name="expr"/> from the row one position before the current row
    /// in the window.
    /// </summary>
    public static AnalyticLagFunction Lag(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>LAG(expr, offset)</c> analytic function: the value of
    /// <paramref name="expr"/> from the row <paramref name="offset"/> positions
    /// before the current row. The offset is emitted as an integer literal (not
    /// a bind parameter), because some databases (e.g. MySQL) require a constant.
    /// </summary>
    public static AnalyticLagFunction Lag(object expr, int offset) =>
        new(Resolve(expr), offset);

    /// <summary>
    /// The <c>LAG(expr, offset, default)</c> analytic function: the value of
    /// <paramref name="expr"/> from the row <paramref name="offset"/> positions
    /// before the current row, or <paramref name="defaultValue"/> when that row
    /// falls outside the partition. The offset is emitted as an integer literal
    /// (some databases, e.g. MySQL, require a constant); the default value is
    /// parameterized.
    /// </summary>
    public static AnalyticLagFunction Lag(
        object expr,
        int offset,
        object defaultValue) => new(
            Resolve(expr),
            offset,
            Resolve(defaultValue));

    public static LastDayFunction LastDay(object date) =>
        new(Resolve(date));

    /// <summary>
    /// The <c>LEAD(expr)</c> analytic function: the value of
    /// <paramref name="expr"/> from the row one position after the current row
    /// in the window.
    /// </summary>
    public static AnalyticLeadFunction Lead(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>LEAD(expr, offset)</c> analytic function: the value of
    /// <paramref name="expr"/> from the row <paramref name="offset"/> positions
    /// after the current row. The offset is emitted as an integer literal (not a
    /// bind parameter), because some databases (e.g. MySQL) require a constant.
    /// </summary>
    public static AnalyticLeadFunction Lead(object expr, int offset) =>
        new(Resolve(expr), offset);

    /// <summary>
    /// The <c>LEAD(expr, offset, default)</c> analytic function: the value of
    /// <paramref name="expr"/> from the row <paramref name="offset"/> positions
    /// after the current row, or <paramref name="defaultValue"/> when that row
    /// falls outside the partition. The offset is emitted as an integer literal
    /// (some databases, e.g. MySQL, require a constant); the default value is
    /// parameterized.
    /// </summary>
    public static AnalyticLeadFunction Lead(
        object expr,
        int offset,
        object defaultValue) => new(
            Resolve(expr),
            offset,
            Resolve(defaultValue));

    public static LeastFunction Least(params object[] expressions) =>
        new(Resolve(expressions));

    public static LengthFunction Length(object source) =>
        new(Resolve(source));

    public static LengthbFunction Lengthb(object source) =>
        new(Resolve(source));

    public static LowerFunction Lower(object source) =>
        new(Resolve(source));

    public static LpadFunction Lpad(
        object source,
        object length) => new(
            Resolve(source),
            Resolve(length));

    public static LpadFunction Lpad(
        object source,
        object length,
        object padding) => new(
            Resolve(source),
            Resolve(length),
            Resolve(padding));

    public static LtrimFunction Ltrim(object source) =>
        new(Resolve(source));

    public static LtrimFunction Ltrim(
        object source,
        object trimChars) => new(
            Resolve(source),
            Resolve(trimChars));
}
