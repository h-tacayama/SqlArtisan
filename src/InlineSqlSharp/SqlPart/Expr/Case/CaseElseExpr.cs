namespace InlineSqlSharp;

public sealed class CaseElseExpr : AbstractExpr
{
    private readonly AbstractExpr _elseExpr;

    internal CaseElseExpr(AbstractExpr elseExpr)
    {
        _elseExpr = elseExpr;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(_elseExpr);
}
