namespace SqlArtisan.Internal;

// A numeric ORDER BY literal. Which values are out of domain depends on the
// position and the engine, so each one travels as a flag for Validate(Dbms)
// rather than throwing at the call.
internal sealed class NumericSortKey(
    string text, bool fractional, bool zeroOrdinal, bool negativeOrdinal) : SqlExpression
{
    internal bool IsFractional => fractional;

    internal bool IsZeroOrdinal => zeroOrdinal;

    internal bool IsNegativeOrdinal => negativeOrdinal;

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(text);
}
