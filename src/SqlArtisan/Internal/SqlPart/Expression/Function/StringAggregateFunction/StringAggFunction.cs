namespace SqlArtisan.Internal;

/// <summary>
/// The <c>STRING_AGG(expr, separator)</c> string aggregate (PostgreSQL and
/// SQL Server). Ordering is dialect-specific: PostgreSQL takes an inline
/// <c>ORDER BY</c> via <see cref="OrderBy(object[])"/>, while SQL Server uses
/// <c>WITHIN GROUP (ORDER BY ...)</c> via <see cref="WithinGroup(OrderByClause)"/>.
/// </summary>
public sealed class StringAggFunction : SqlExpression
{
    private readonly SqlExpression _expr;
    private readonly SqlExpression _separator;
    private OrderByClause? _orderBy;
    private WithinGroupClause? _withinGroup;

    internal StringAggFunction(SqlExpression expr, SqlExpression separator)
    {
        _expr = expr;
        _separator = separator;
    }

    /// <summary>
    /// Adds an inline <c>ORDER BY</c> inside the call:
    /// <c>STRING_AGG(expr, sep ORDER BY ...)</c> (PostgreSQL).
    /// </summary>
    public StringAggFunction OrderBy(params object[] orderByItems)
    {
        _orderBy = OrderByClause.Parse(orderByItems);
        return this;
    }

    /// <summary>
    /// Adds a <c>WITHIN GROUP (ORDER BY ...)</c> clause after the call:
    /// <c>STRING_AGG(expr, sep) WITHIN GROUP (ORDER BY ...)</c> (SQL Server).
    /// </summary>
    public StringAggFunction WithinGroup(OrderByClause orderByClause)
    {
        _withinGroup = new WithinGroupClause(orderByClause);
        return this;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.StringAgg)
        .OpenParenthesis()
        .Append(_expr)
        .PrependComma(_separator)
        .PrependSpaceIfNotNull(_orderBy)
        .CloseParenthesis()
        .PrependSpaceIfNotNull(_withinGroup);
}
