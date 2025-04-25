namespace InlineSqlSharp;

public sealed class NvlFunction : AbstractExpr
{
    private readonly AbstractSqlPart _expr1;
    private readonly AbstractSqlPart _expr2;

    internal NvlFunction(AbstractExpr expr1, AbstractExpr expr2)
    {
        _expr1 = expr1;
        _expr2 = expr2;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.NVL)
        .OpenParenthesis()
        .Append(_expr1)
        .PrependComma(_expr2)
        .CloseParenthesis();
}
