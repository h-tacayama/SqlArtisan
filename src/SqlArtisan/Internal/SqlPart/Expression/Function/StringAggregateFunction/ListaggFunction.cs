namespace SqlArtisan.Internal;

/// <summary>
/// The <c>LISTAGG(expr, separator)</c> string aggregate (Oracle), pending its
/// <c>WITHIN GROUP (ORDER BY ...)</c> clause: complete the call with
/// <see cref="WithinGroup(OrderByClause)"/> before it can be used as an expression.
/// </summary>
/// <remarks>
/// Oracle XE 21.3.0 runs the call without the clause, but <c>OrderBy(Null)</c>
/// aggregates the same values with the order left to Oracle, so the pending type
/// withholds no query (<c>public-api-design.md</c> § "Opinions…", #664).
/// </remarks>
public sealed class ListaggFunction : IIncompleteExpression
{
    private readonly SqlExpression _expr;
    private readonly SqlExpression _separator;

    internal ListaggFunction(SqlExpression expr, SqlExpression separator)
    {
        _expr = expr;
        _separator = separator;
    }

    string IIncompleteExpression.IncompleteMessage =>
        "LISTAGG requires a WITHIN GROUP clause; complete it with .WithinGroup(OrderBy(...)).";

    /// <summary>
    /// Supplies Oracle's <c>WITHIN GROUP (ORDER BY ...)</c> clause:
    /// <c>LISTAGG(expr, sep) WITHIN GROUP (ORDER BY ...)</c>.
    /// </summary>
    public ListaggWithinGroupFunction WithinGroup(OrderByClause orderByClause) =>
        new(_expr, _separator, new WithinGroupClause(orderByClause));
}
