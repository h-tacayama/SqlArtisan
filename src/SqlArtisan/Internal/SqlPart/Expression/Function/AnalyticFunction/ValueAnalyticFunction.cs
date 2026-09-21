namespace SqlArtisan.Internal;

/// <summary>
/// Base class for a value analytic function, which extends the ordered window
/// with an optional explicit frame.
/// </summary>
public abstract class ValueAnalyticFunction : AnalyticFunction
{
    private protected ValueAnalyticFunction()
    {
    }

    /// <summary>
    /// Turns the analytic function into a window function with an explicit frame:
    /// <c>OVER (... ROWS/RANGE ...)</c>.
    /// </summary>
    public WindowFunction Over(WindowFrameClause windowFrameClause) =>
        new(this, OverClause.Of(windowFrameClause));
}
