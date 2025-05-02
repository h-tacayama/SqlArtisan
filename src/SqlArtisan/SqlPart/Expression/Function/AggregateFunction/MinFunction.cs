namespace SqlArtisan;

public sealed class MinFunction : SqlExpression
{
    private readonly SqlPart _expr;

    internal MinFunction(SqlExpression expr)
    {
        _expr = expr;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Min)
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
