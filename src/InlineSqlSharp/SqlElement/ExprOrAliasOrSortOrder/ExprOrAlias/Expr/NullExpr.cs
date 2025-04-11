namespace InlineSqlSharp;

public sealed class NullExpr : IAliasable, IExpr
{
    public void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.NULL);

    public void FormatAsSelect(SqlBuildingBuffer buffer) =>
        FormatSql(buffer);

    public override bool Equals(object? obj) => base.Equals(obj);

    public override int GetHashCode() => base.GetHashCode();

    public ExprAlias AS(string alias) => new(this, alias);
}
