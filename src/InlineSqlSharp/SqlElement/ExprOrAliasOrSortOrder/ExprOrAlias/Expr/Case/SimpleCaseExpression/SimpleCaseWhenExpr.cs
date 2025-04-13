namespace InlineSqlSharp;

public sealed class SimpleCaseWhenExpr(object whenExpr) : ISqlElement
{
    private readonly object _whenExpr = whenExpr;

    public SimpleCaseWhenClause THEN(object thenExpr) =>
        new(this, new CaseThenExpr(thenExpr));

    public void FormatSql(SqlBuildingBuffer buffer) => buffer.Append(_whenExpr);
}
