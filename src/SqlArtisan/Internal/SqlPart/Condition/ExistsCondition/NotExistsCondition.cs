namespace SqlArtisan.Internal;

public sealed class NotExistsCondition : SqlCondition
{
    private readonly ISubquery _subquery;

    internal NotExistsCondition(ISubquery subquery)
    {
        _subquery = subquery;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Not} {Keywords.Exists} ")
        .EncloseInParentheses(_subquery);
}
