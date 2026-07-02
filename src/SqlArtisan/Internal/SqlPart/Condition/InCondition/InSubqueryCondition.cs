namespace SqlArtisan.Internal;

public sealed class InSubqueryCondition : SqlCondition
{
    private readonly SqlExpression _leftSide;
    private readonly SqlPartAgent _subquery;

    internal InSubqueryCondition(SqlExpression leftSide, ISubquery subquey)
    {
        _leftSide = leftSide;
        _subquery = new(subquey.Format);
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(_leftSide)
        .EncloseInSpaces(Keywords.In)
        .EncloseInParentheses(_subquery);
}
