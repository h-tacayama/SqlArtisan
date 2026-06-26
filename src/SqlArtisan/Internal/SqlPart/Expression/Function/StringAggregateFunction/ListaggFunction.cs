namespace SqlArtisan.Internal;

/// <summary>
/// The <c>LISTAGG(expr, separator)</c> string aggregate (Oracle), pending its
/// mandatory <c>WITHIN GROUP (ORDER BY ...)</c> clause. Oracle requires the
/// ordering, so complete the call with <see cref="WithinGroup(OrderByClause)"/>
/// before it can be used as an expression.
/// </summary>
public sealed class ListaggFunction
{
    private readonly SqlExpression _expr;
    private readonly SqlExpression _separator;

    internal ListaggFunction(SqlExpression expr, SqlExpression separator)
    {
        _expr = expr;
        _separator = separator;
    }

    /// <summary>
    /// Supplies Oracle's mandatory <c>WITHIN GROUP (ORDER BY ...)</c> clause:
    /// <c>LISTAGG(expr, sep) WITHIN GROUP (ORDER BY ...)</c>.
    /// </summary>
    public ListaggWithinGroupFunction WithinGroup(OrderByClause orderByClause) =>
        new(_expr, _separator, new WithinGroupClause(orderByClause));
}
