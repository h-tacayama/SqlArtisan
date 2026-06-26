using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// The <c>FIRST_VALUE(expr)</c> analytic function: the value of
    /// <paramref name="expr"/> from the first row of the window frame.
    /// </summary>
    /// <param name="expr">The expression to read from the first row of the frame.</param>
    /// <returns>An <see cref="AnalyticFirstValueFunction"/> emitting
    /// <c>FIRST_VALUE(expr)</c>.</returns>
    public static AnalyticFirstValueFunction FirstValue(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// The <c>FLOOR(expr)</c> function (largest integer not greater than
    /// <paramref name="expr"/>).
    /// </summary>
    /// <param name="expr">The numeric expression to round down.</param>
    /// <returns>A <see cref="FloorFunction"/> emitting <c>FLOOR(expr)</c>.</returns>
    public static FloorFunction Floor(object expr) =>
        new(Resolve(expr));

    /// <summary>
    /// A <c>n FOLLOWING</c> window-frame bound (offset rows/range after the
    /// current row).
    /// </summary>
    /// <param name="offset">The number of rows/range units after the current row.</param>
    /// <returns>A <see cref="FrameBound"/> for the <c>n FOLLOWING</c> bound.</returns>
    public static FrameBound Following(int offset) => FrameBound.Following(offset);
}
