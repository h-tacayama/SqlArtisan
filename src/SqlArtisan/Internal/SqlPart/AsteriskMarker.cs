namespace SqlArtisan.Internal;

/// <summary>
/// The bare <c>*</c> marker — <c>COUNT(*)</c>'s argument and the <c>SELECT *</c>
/// select item. Deliberately not a <see cref="SqlExpression"/>, so it cannot
/// reach other expression positions (<c>UPPER(*)</c> does not compile).
/// </summary>
public sealed class AsteriskMarker : SqlPart
{
    internal AsteriskMarker() { }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(Operators.Asterisk);
}
