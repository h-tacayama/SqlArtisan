namespace SqlArtisan.Internal;

/// <summary>
/// The bare <c>*</c> marker — <c>COUNT(*)</c>'s argument and the <c>SELECT *</c>
/// select item. Deliberately not a <see cref="SqlExpression"/>, so an expression
/// position rejects it eagerly: <c>UPPER(*)</c> throws at the call, not at
/// <c>Build()</c>.
/// </summary>
public sealed class AsteriskMarker : SqlPart
{
    internal AsteriskMarker() { }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(Operators.Asterisk);
}
