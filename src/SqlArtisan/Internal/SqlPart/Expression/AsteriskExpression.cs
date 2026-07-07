namespace SqlArtisan.Internal;

/// <summary>
/// The <c>*</c> marker inside <c>COUNT(*)</c>.
/// </summary>
internal sealed class AsteriskExpression : SqlPart
{
    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(Operators.Asterisk);
}
