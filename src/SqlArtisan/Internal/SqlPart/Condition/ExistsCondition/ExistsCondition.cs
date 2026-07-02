namespace SqlArtisan.Internal;

public sealed class ExistsCondition : SqlCondition
{
    private readonly ISubquery _subquery;

    internal ExistsCondition(ISubquery subquery)
    {
        _subquery = subquery;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Exists} ")
        .EncloseInParentheses(_subquery);
}
