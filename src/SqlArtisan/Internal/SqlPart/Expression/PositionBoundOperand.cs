namespace SqlArtisan.Internal;

// The value-position face of a node valid in one operand slot alone (ANY / ALL / SOME,
// MySQL's INTERVAL): only that slot's resolvers wrap one, at 24 B a use (ADR 0006), so
// the node stays no SqlExpression and every other position rejects it (#666).
internal sealed class PositionBoundOperand(SqlPart operand) : SqlExpression
{
    internal SqlPart Operand { get; } = operand;

    // LIKE takes an ANY / ALL / SOME pattern and ESCAPE a plain one, never both (#666).
    internal static SqlExpression ThrowIfQuantifiedPattern(SqlExpression pattern, string like) =>
        pattern is PositionBoundOperand
            ? throw new ArgumentException(
                $"{like} ANY, ALL or SOME takes no ESCAPE; write one {like} ... ESCAPE per "
                    + "pattern.")
            : pattern;

    internal override void Format(SqlBuildingBuffer buffer) => buffer.Append(Operand);
}
