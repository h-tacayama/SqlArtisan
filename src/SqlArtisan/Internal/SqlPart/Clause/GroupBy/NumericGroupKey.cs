namespace SqlArtisan.Internal;

// A numeric GROUP BY literal. Unlike ORDER BY's, an out-of-range ordinal is
// rejected at the call (every engine refuses it), so only the meaningful
// values reach here and the node has no verdict left to carry.
internal sealed class NumericGroupKey(string text) : SqlExpression
{
    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(text);
}
