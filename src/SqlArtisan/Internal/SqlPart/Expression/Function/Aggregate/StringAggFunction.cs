namespace SqlArtisan.Internal;

// PostgreSQL STRING_AGG(expr, sep ORDER BY ord): separator is the 2nd argument
// and the ORDER BY rides *inside* the call. Distinct from the WITHIN GROUP form
// (Oracle / SQL Server) — same intent, different structure, hence a separate node.
// Each node is a fixed form, so it never branches on Dbms (CLAUDE.md); the
// per-DBMS namespace simply picks the right node.
public sealed class StringAggFunction : SqlExpression
{
    private readonly SqlExpression _expr;
    private readonly SqlExpression _separator;
    private readonly SqlExpression _orderBy;

    internal StringAggFunction(
        SqlExpression expr,
        SqlExpression separator,
        SqlExpression orderBy,
        Dbms dbms) : base(dbms)
    {
        _expr = expr;
        _separator = separator;
        _orderBy = orderBy;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.StringAgg)
        .OpenParenthesis()
        .Append(_expr)
        .PrependComma(_separator)
        .Append($" {Keywords.Order} {Keywords.By} ")
        .Append(_orderBy)
        .CloseParenthesis();
}
