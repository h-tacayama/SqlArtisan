namespace SqlArtisan;

public sealed class ModFunction : AbstractExpr
{
    private readonly AbstractSqlPart _dividend;
    private readonly AbstractSqlPart _divisor;

    internal ModFunction(AbstractExpr dividend, AbstractExpr divisor)
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
