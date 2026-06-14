using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// The <c>FIRST_VALUE(expr)</c> analytic function: the value of
    /// <paramref name="expr"/> from the first row of the window frame.
    /// </summary>
    public static AnalyticFirstValueFunction FirstValue(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>FLOOR(expr)</c> function (largest integer not greater than
    /// <paramref name="expr"/>).
    /// </summary>
    public static FloorFunction Floor(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// A <c>n FOLLOWING</c> window-frame bound (offset rows/range after the
    /// current row).
    /// </summary>
    public static FrameBound Following(int offset) => FrameBound.Following(offset);
}
