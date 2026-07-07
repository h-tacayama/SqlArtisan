namespace SqlArtisan.Internal;

public sealed class DoublePipeOperator : SqlExpression
{
    private readonly OperatorJoinedFunctionCore _core;

    internal DoublePipeOperator(SqlExpression primary, SqlExpression secondary, SqlExpression[] others)
    {
        _core = new(Operators.DoublePipe, [primary, secondary, .. others]);
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        _core.Format(buffer);
}
