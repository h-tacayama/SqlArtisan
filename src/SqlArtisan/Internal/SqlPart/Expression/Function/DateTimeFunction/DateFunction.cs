namespace SqlArtisan.Internal;

public sealed class DateFunction : SqlExpression
{
    private readonly VariadicFunctionCore _core;

    internal DateFunction(SqlExpression timevalue)
    {
        _core = new(Keywords.Date, timevalue);
    }

    internal DateFunction(SqlExpression[] args)
    {
        _core = new(Keywords.Date, args);
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        _core.Format(buffer);
}
