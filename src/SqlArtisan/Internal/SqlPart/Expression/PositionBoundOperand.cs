namespace SqlArtisan.Internal;

// The value-position face of a node valid in one operand slot alone (ANY / ALL / SOME,
// MySQL's INTERVAL): only that slot's resolvers wrap one, at 24 B a use (ADR 0006), so
// the node stays no SqlExpression and every other position rejects it (#666).
internal sealed class PositionBoundOperand(SqlPart operand) : SqlExpression
{
    internal SqlPart Operand { get; } = operand;

    internal override void Format(SqlBuildingBuffer buffer) => buffer.Append(Operand);
}
