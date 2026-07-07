namespace SqlArtisan.Internal;

/// <summary>
/// The <c>*</c> marker inside <c>COUNT(*)</c>. Stateless, so a single shared
/// instance keeps the parameterless <see cref="CountFunction"/> build path
/// allocation-free (ADR 0006).
/// </summary>
internal sealed class AsteriskExpression : SqlPart
{
    internal static readonly AsteriskExpression Instance = new();

    private AsteriskExpression()
    {
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.Append(Operators.Asterisk);
}
