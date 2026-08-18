namespace SqlArtisan.Internal;

public sealed class ArrayConstructorExpression : SqlExpression
{
    private readonly SqlExpression[] _elements;

    internal ArrayConstructorExpression(SqlExpression[] elements)
    {
        _elements = elements;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Array)
        .Append('[')
        .AppendCsv(_elements)
        .Append(']');
}
