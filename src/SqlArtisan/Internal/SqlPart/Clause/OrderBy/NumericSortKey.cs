namespace SqlArtisan.Internal;

// A numeric ORDER BY literal: an integer is a column ordinal on every engine,
// while a fractional value is a constant sort key that PostgreSQL rejects
// outright — the flag lets Validate(Dbms) tell the two apart.
internal sealed class NumericSortKey(string text, bool fractional) : SqlExpression
{
    internal bool IsFractional => fractional;

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(text);
}
