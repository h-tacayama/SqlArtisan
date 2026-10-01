namespace SqlArtisan.Internal;

/// <summary>
/// The <c>STRING_AGG(expr, separator ORDER BY ...)</c> string aggregate with an inline
/// ordering (PostgreSQL, SQLite 3.44+).
/// </summary>
public sealed class StringAggOrderByFunction : SqlExpression
{
    private readonly SqlExpression _expr;
    private readonly string _separator;
    private readonly OrderByClause _orderByClause;

    internal StringAggOrderByFunction(
        SqlExpression expr,
        string separator,
        OrderByClause orderByClause)
    {
        ArgumentNullException.ThrowIfNull(separator);

        _expr = expr;
        _separator = separator;
        _orderByClause = orderByClause;
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        StringAggFunction.FormatCall(buffer, _expr, _separator, _orderByClause);
}
