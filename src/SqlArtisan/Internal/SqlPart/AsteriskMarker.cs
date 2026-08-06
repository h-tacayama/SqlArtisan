namespace SqlArtisan.Internal;

/// <summary>
/// The bare <c>*</c> marker — <c>COUNT(*)</c>'s argument and the <c>SELECT *</c>
/// select item. Deliberately not a <see cref="SqlExpression"/>, so an expression
/// position rejects it: <c>UPPER(*)</c> throws at build.
/// </summary>
public sealed class AsteriskMarker : SqlPart
{
    internal AsteriskMarker() { }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(Operators.Asterisk);
}
