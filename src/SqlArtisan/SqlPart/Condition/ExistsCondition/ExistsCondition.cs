namespace SqlArtisan;

public sealed class ExistsCondition : SqlCondition
{
    private readonly SqlPartAgent _subquery;

    internal ExistsCondition(ISubquery subquery)
    {
        _subquery = new(subquery.FormatSql);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Exists} ")
        .EncloseInParentheses(_subquery);
}
