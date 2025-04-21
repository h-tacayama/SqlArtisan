namespace InlineSqlSharp;

public sealed class CaseElseExpr(AbstractExpr elseExpr) : AbstractExpr
{
    private readonly AbstractExpr _elseExpr = elseExpr;

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(_elseExpr);
}
