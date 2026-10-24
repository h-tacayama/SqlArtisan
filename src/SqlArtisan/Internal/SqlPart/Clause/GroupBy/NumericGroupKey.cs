namespace SqlArtisan.Internal;

// A numeric GROUP BY or DISTINCT ON literal. Unlike ORDER BY's, an ordinal below 1
// is rejected at the call (it names no select-list position), so the node has no
// verdict left to carry.
internal sealed class NumericGroupKey(string text) : SqlExpression
{
    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(text);
}
