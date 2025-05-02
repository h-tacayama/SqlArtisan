namespace SqlArtisan;

public sealed class NullExpression : SqlExpression
{
    internal NullExpression() { }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.Null);
}
