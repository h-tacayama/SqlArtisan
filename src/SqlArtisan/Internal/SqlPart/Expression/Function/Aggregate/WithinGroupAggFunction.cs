namespace SqlArtisan.Internal;

// FUNC(expr, sep) WITHIN GROUP (ORDER BY ord) — shared by Oracle (LISTAGG) and
// SQL Server (STRING_AGG). The structure is identical; only the function name
// differs, so one node serves both DBMS (the name is passed in). A small example
// of partial reuse across dialects that share a construct.
public sealed class WithinGroupAggFunction : SqlExpression
{
    private readonly string _functionName;
    private readonly SqlExpression _expr;
    private readonly SqlExpression _separator;
    private readonly SqlExpression _orderBy;

    internal WithinGroupAggFunction(
        string functionName,
        SqlExpression expr,
        SqlExpression separator,
        SqlExpression orderBy,
        Dbms dbms) : base(dbms)
    {
        _functionName = functionName;
        _expr = expr;
        _separator = separator;
        _orderBy = orderBy;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_functionName)
        .OpenParenthesis()
        .Append(_expr)
        .PrependComma(_separator)
        .CloseParenthesis()
        .Append($" {Keywords.WithinGroup} ")
        .OpenParenthesis()
        .Append($"{Keywords.Order} {Keywords.By} ")
        .Append(_orderBy)
        .CloseParenthesis();
}
