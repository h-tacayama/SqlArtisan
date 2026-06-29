namespace SqlArtisan.Internal;

public sealed class WindowFunction : SqlExpression
{
    private readonly SqlPart _function;
    private readonly OverClause _overClause;

    internal WindowFunction(
        SqlPart function,
        OverClause overClause)
    {
        _function = function;
        _overClause = overClause;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(_function)
        .Append(_overClause);
}
