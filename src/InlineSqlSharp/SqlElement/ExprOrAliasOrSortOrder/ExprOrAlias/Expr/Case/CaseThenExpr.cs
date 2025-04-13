namespace InlineSqlSharp;
public sealed class CaseThenExpr(object thenExpr) : ISqlElement
{
    private readonly object _thenExpr = thenExpr;

    public void FormatSql(SqlBuildingBuffer buffer) => buffer.Append(_thenExpr);
}
