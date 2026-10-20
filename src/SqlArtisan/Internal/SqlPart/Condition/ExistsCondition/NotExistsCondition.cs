namespace SqlArtisan.Internal;

internal sealed class NotExistsCondition : SqlCondition
{
    private readonly ISubquery _subquery;

    internal NotExistsCondition(ISubquery subquery)
    {
        _subquery = SqlBuilderBase.Composed(subquery);
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Not} {Keywords.Exists} ")
        .EncloseInParentheses(_subquery);
}
