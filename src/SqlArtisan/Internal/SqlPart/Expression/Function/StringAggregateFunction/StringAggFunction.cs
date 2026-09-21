namespace SqlArtisan.Internal;

/// <summary>
/// The <c>STRING_AGG(expr, separator)</c> string aggregate (PostgreSQL, SQLite 3.44+,
/// SQL Server). PostgreSQL and SQLite take an inline <c>ORDER BY</c> argument; SQL Server
/// orders through a trailing <see cref="WithinGroup(OrderByClause)"/>.
/// </summary>
public sealed class StringAggFunction : SqlExpression
{
    private readonly SqlExpression _expr;
    private readonly string _separator;
    private readonly OrderByClause? _orderByClause;

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
    /// Returns the call with a trailing <c>WITHIN GROUP (ORDER BY ...)</c> clause:
    /// <c>STRING_AGG(expr, sep) WITHIN GROUP (ORDER BY ...)</c> (SQL Server).
    /// This instance is unchanged.
    /// </summary>
    /// <param name="orderByClause">The ordering, built with <c>Sql.OrderBy(...)</c>.</param>
    /// <returns>A new expression emitting the call followed by the clause.</returns>
    /// <exception cref="ArgumentException">The call already carries an inline <c>ORDER BY</c> argument.</exception>
    public StringAggWithinGroupFunction WithinGroup(OrderByClause orderByClause)
    {
        // The combined shape has no valid spelling on any dialect; the ordering
        // is fixed at the call, so it throws eagerly.
        if (_orderByClause is not null)
        {
            throw new ArgumentException(
                "STRING_AGG cannot combine an inline ORDER BY argument with WITHIN GROUP (ORDER BY "
                    + "...); use one or the other.");
        }

        return new StringAggWithinGroupFunction(this, new WithinGroupClause(orderByClause));
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
        .CloseParenthesis();
}
