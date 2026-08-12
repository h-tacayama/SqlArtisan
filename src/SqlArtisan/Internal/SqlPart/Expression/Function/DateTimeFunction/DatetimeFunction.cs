namespace SqlArtisan.Internal;

public sealed class DatetimeFunction : SqlExpression
{
    private readonly VariadicFunctionCore _core;

    internal DatetimeFunction(SqlExpression[] args)
    {
        _core = new(Keywords.Datetime, args);
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        _core.Format(buffer);
}
