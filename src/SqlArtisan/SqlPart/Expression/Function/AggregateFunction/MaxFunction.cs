namespace SqlArtisan;

public sealed class MaxFunction : SqlExpression
{
    private readonly SqlPart _expr;

    internal MaxFunction(SqlExpression expr)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Max)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
