namespace SqlArtisan.Internal;

public sealed class DoublePipeOperator : SqlExpression
{
    private readonly OperatorJoinedFunctionCore _core;

    // Takes the pre-merged operand array (ResolveVariadic) rather than merging a
    // params tail itself — the spread would allocate a second array (ADR 0006).
    internal DoublePipeOperator(SqlExpression[] operands)
    {
        _core = new(Operators.DoublePipe, operands);
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        _core.Format(buffer);
}
