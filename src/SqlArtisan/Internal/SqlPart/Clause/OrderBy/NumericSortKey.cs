namespace SqlArtisan.Internal;

// A numeric statement ORDER BY literal. The zero flag is checked at the
// OrderBy call (no engine resolves it); the fractional and negative ones depend
// on the engine, so they travel to Validate(Dbms).
internal sealed class NumericSortKey(
    string text, bool fractional, bool zeroOrdinal, bool negativeOrdinal) : SqlExpression
{
    internal bool IsFractional => fractional;

    internal bool IsZeroOrdinal => zeroOrdinal;

    internal bool IsNegativeOrdinal => negativeOrdinal;

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(text);
}
