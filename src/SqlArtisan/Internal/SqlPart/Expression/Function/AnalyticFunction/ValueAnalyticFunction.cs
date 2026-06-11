namespace SqlArtisan.Internal;

/// <summary>
/// Base class for the value analytic functions (<c>FIRST_VALUE</c>,
/// <c>LAST_VALUE</c>, <c>NTH_VALUE</c>), which extend the ordered window with an
/// optional explicit frame.
/// </summary>
public abstract class ValueAnalyticFunction : AnalyticFunction
{
    /// <summary>
    /// Turns the analytic function into a window function with an explicit frame:
    /// <c>OVER (... ROWS/RANGE ...)</c>.
    /// </summary>
    public WindowFunction Over(WindowFrameClause windowFrameClause) =>
        new(this, OverClause.Of(windowFrameClause));
}
