namespace SqlArtisan.Internal;

public sealed class NotInSubqueryCondition : SqlCondition
{
    private readonly SqlExpression _leftSide;
    private readonly ISubquery _subquery;

    internal NotInSubqueryCondition(SqlExpression leftSide, ISubquery subquery)
    {
        _leftSide = leftSide;
        _subquery = subquery;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .EncloseInSpaces($"{Keywords.Not} {Keywords.In}")
        .EncloseInParentheses(_subquery);
}
