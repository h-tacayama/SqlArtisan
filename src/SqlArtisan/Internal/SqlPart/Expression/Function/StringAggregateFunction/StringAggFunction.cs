namespace SqlArtisan.Internal;

/// <summary>
/// The <c>STRING_AGG(expr, separator)</c> string aggregate (PostgreSQL,
/// SQLite 3.44+, and SQL Server). Ordering is dialect-specific: PostgreSQL and
/// SQLite take an inline <c>ORDER BY</c> passed as an argument to
/// <c>Sql.StringAgg(...)</c> (it sits inside the call), while SQL Server uses a
/// trailing <c>WITHIN GROUP (ORDER BY ...)</c> via
/// <see cref="WithinGroup(OrderByClause)"/>.
/// </summary>
public sealed class StringAggFunction : SqlExpression
{
    private readonly SqlExpression _expr;
    private readonly string _separator;
    private readonly OrderByClause? _orderByClause;
    private WithinGroupClause? _withinGroupClause;

    internal StringAggFunction(
        SqlExpression expr,
        string separator,
        OrderByClause? orderByClause = null)
    {
        ArgumentNullException.ThrowIfNull(separator);

        _expr = expr;
        _separator = separator;
        _orderByClause = orderByClause;
    }

    /// <summary>
    /// Adds a trailing <c>WITHIN GROUP (ORDER BY ...)</c> clause after the call:
    /// <c>STRING_AGG(expr, sep) WITHIN GROUP (ORDER BY ...)</c> (SQL Server).
    /// </summary>
    public StringAggFunction WithinGroup(OrderByClause orderByClause)
    {
        _withinGroupClause = new WithinGroupClause(orderByClause);
        return this;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.StringAgg)
        .OpenParenthesis()
        .Append(_expr)
        // Inlined as a string literal, never bound: SQL Server requires
        // STRING_AGG's separator to be a literal (ADR 0004, #168).
        .Append(", ")
        .AppendStringLiteral(_separator)
        .PrependSpaceIfNotNull(_orderByClause)
        .CloseParenthesis()
        .PrependSpaceIfNotNull(_withinGroupClause);
}
