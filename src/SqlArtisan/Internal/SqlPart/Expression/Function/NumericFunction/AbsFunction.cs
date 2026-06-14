namespace SqlArtisan.Internal;

public sealed class AbsFunction : SqlExpression
{
    private readonly SqlExpression _expr;

    internal AbsFunction(SqlExpression expr)
    {
        _expr = expr;
    }

    // Tagged overload used by the per-DBMS namespaces (e.g. SqlArtisan.Oracle).
    internal AbsFunction(SqlExpression expr, Dbms dbms) : base(dbms)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Abs)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
