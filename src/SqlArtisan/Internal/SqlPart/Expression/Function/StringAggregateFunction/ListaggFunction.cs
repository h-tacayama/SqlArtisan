namespace SqlArtisan.Internal;

/// <summary>
/// The <c>LISTAGG(expr, separator)</c> string aggregate (Oracle). Oracle orders
/// the aggregated values with a mandatory <c>WITHIN GROUP (ORDER BY ...)</c>
/// clause, supplied via <see cref="WithinGroup(OrderByClause)"/>.
/// </summary>
public sealed class ListaggFunction : SqlExpression
{
    private readonly SqlExpression _expr;
    private readonly SqlExpression _separator;
    private WithinGroupClause? _withinGroup;

    internal ListaggFunction(SqlExpression expr, SqlExpression separator)
    {
        _expr = expr;
        _separator = separator;
    }

    /// <summary>
    /// Adds the <c>WITHIN GROUP (ORDER BY ...)</c> clause:
    /// <c>LISTAGG(expr, sep) WITHIN GROUP (ORDER BY ...)</c> (Oracle).
    /// </summary>
    public ListaggFunction WithinGroup(OrderByClause orderByClause)
    {
        _withinGroup = new WithinGroupClause(orderByClause);
        return this;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Listagg)
        .OpenParenthesis()
        .Append(_expr)
        .PrependComma(_separator)
        .CloseParenthesis()
        .PrependSpaceIfNotNull(_withinGroup);
}
