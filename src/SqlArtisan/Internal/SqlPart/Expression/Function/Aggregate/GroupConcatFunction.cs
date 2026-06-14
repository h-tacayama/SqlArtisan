namespace SqlArtisan.Internal;

// MySQL GROUP_CONCAT(expr ORDER BY ord SEPARATOR 'lit'): ordering is inline and
// the separator uses the SEPARATOR keyword. Crucially the separator MUST be a
// string literal — MySQL rejects a bind parameter there — so it is emitted inline
// rather than parameterised. That is a concrete "discoverability != correctness"
// divergence the namespace surfaces but cannot itself fix.
public sealed class GroupConcatFunction : SqlExpression
{
    private readonly SqlExpression _expr;
    private readonly SqlExpression _orderBy;
    private readonly string _separator;

    internal GroupConcatFunction(
        SqlExpression expr,
        SqlExpression orderBy,
        string separator,
        Dbms dbms) : base(dbms)
    {
        _expr = expr;
        _orderBy = orderBy;
        _separator = separator;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.GroupConcat)
        .OpenParenthesis()
        .Append(_expr)
        .Append($" {Keywords.Order} {Keywords.By} ")
        .Append(_orderBy)
        .Append($" {Keywords.Separator} '")
        .Append(_separator)
        .Append('\'')
        .CloseParenthesis();
}
