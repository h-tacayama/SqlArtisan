namespace SqlArtisan.Internal;

internal sealed class NotExistsCondition : SqlCondition
{
    private readonly ISubquery _subquery;

    internal NotExistsCondition(ISubquery subquery)
    {
        _subquery = SqlBuilderBase.MarkComposed(subquery);
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Not} {Keywords.Exists} ")
        .EncloseInParentheses(_subquery);
}
