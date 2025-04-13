namespace InlineSqlSharp;

public sealed class NullExpr : IExpr
{
    public ExprAlias AS(string alias) => new(this, alias);

    public void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.NULL);

    public void FormatAsSelect(SqlBuildingBuffer buffer) =>
        FormatSql(buffer);
}
