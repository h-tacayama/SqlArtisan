namespace SqlArtisan.Internal;

internal sealed class ExistsCondition : SqlCondition
{
    private readonly ISubquery _subquery;

    internal ExistsCondition(ISubquery subquery)
    {
        _subquery = SqlBuilderBase.Composed(subquery);
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Exists} ")
        .EncloseInParentheses(_subquery);
}
