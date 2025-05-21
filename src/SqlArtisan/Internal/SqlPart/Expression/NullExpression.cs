namespace SqlArtisan.Internal;

public sealed class NullExpression : SqlExpression
{
    internal NullExpression() { }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(Keywords.Null);
}
