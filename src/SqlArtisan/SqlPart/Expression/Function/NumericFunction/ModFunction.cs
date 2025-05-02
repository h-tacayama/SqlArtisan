namespace SqlArtisan;

public sealed class ModFunction : SqlExpression
{
    private readonly SqlExpression _dividend;
    private readonly SqlExpression _divisor;

    internal ModFunction(SqlExpression dividend, SqlExpression divisor)
    {
        _dividend = dividend;
        _divisor = divisor;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Mod)
        .OpenParenthesis()
        .Append(_dividend)
        .PrependComma(_divisor)
        .CloseParenthesis();
}
