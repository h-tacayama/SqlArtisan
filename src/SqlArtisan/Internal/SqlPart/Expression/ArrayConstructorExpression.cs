namespace SqlArtisan.Internal;

// Internal-only for now: it exists as the ?| / ?& right-hand side; a public
// ARRAY[...] factory belongs to the array-operator wave (#159 GAP-14).
public sealed class ArrayConstructorExpression(SqlExpression[] elements) : SqlExpression
{
    private readonly SqlExpression[] _elements = elements;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Array)
        .Append('[')
        .AppendCsv(_elements)
        .Append(']');
}
