namespace SqlArtisan.Internal;

/// <summary>
/// The Oracle <c>LISTAGG(expr, separator) WITHIN GROUP (ORDER BY ...)</c> string
/// aggregate, with its mandatory ordering supplied.
/// </summary>
public sealed class ListaggWithinGroupFunction : SqlExpression
{
    private readonly SqlExpression _expr;
    private readonly SqlExpression _separator;
    private readonly WithinGroupClause _withinGroupClause;

    internal ListaggWithinGroupFunction(
        SqlExpression expr,
        SqlExpression separator,
        WithinGroupClause withinGroupClause)
    {
        _expr = expr;
        _separator = separator;
        _withinGroupClause = withinGroupClause;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Listagg)
        .OpenParenthesis()
        .Append(_expr)
        .PrependComma(_separator)
        .CloseParenthesis()
        .PrependSpace(_withinGroupClause);
}
