namespace SqlArtisan;

public sealed class NullExpr : AbstractExpr
{
    internal NullExpr() { }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.Null);
}
