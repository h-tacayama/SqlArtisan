namespace SqlArtisan.Internal;

public sealed class ExistsCondition : SqlCondition
{
    private readonly SqlPartAgent _subquery;

    internal ExistsCondition(ISubquery subquery)
    {
        _subquery = new(subquery.Format);
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Exists} ")
        .EncloseInParentheses(_subquery);
}
