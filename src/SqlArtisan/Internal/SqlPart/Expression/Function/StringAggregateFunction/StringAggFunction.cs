namespace SqlArtisan.Internal;

/// <summary>
/// The <c>STRING_AGG(expr, separator)</c> string aggregate (PostgreSQL, SQLite 3.44+,
/// SQL Server). SQL Server orders through a trailing <see cref="WithinGroup(OrderByClause)"/>;
/// PostgreSQL's and SQLite's inline ordering is <see cref="StringAggOrderByFunction"/>.
/// </summary>
public sealed class StringAggFunction : SqlExpression
{
    private readonly SqlExpression _expr;
    private readonly string _separator;

    internal StringAggFunction(SqlExpression expr, string separator)
    {
        ArgumentNullException.ThrowIfNull(separator);

        _expr = expr;
        _separator = separator;
    }

    /// <summary>
    /// Returns the call with a trailing <c>WITHIN GROUP (ORDER BY ...)</c> clause:
    /// <c>STRING_AGG(expr, sep) WITHIN GROUP (ORDER BY ...)</c> (SQL Server).
    /// This instance is unchanged.
    /// </summary>
    /// <param name="orderByClause">The ordering, built with <c>Sql.OrderBy(...)</c>.</param>
    /// <returns>A new expression emitting the call followed by the clause.</returns>
    public StringAggWithinGroupFunction WithinGroup(OrderByClause orderByClause) =>
        new(this, new WithinGroupClause(orderByClause));

    internal override void Format(SqlBuildingBuffer buffer) =>
        FormatCall(buffer, _expr, _separator, null);

    internal static void FormatCall(
        SqlBuildingBuffer buffer,
        SqlExpression expr,
        string separator,
        OrderByClause? orderByClause) => buffer
        .Append(Keywords.StringAgg)
        .OpenParenthesis()
        .Append(expr)
        // Inlined as a string literal, never bound: SQL Server requires
        // STRING_AGG's separator to be a literal (ADR 0004, #168).
        .Append(", ")
        .AppendStringLiteral(separator)
        .PrependSpaceIfNotNull(orderByClause)
        .CloseParenthesis();
}
