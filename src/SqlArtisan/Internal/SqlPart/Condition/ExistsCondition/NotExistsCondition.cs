namespace SqlArtisan.Internal;

public sealed class NotExistsCondition : SqlCondition
{
    private readonly SqlPartAgent _subquery;

    internal NotExistsCondition(ISubquery subquery)
    {
        _subquery = new(subquery.Format);
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Not} {Keywords.Exists} ")
        .EncloseInParentheses(_subquery);
}
