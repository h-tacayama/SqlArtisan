namespace InlineSqlSharp;
public sealed class CaseElseExpr<TElse>(TElse elseExpr) : ISqlElement
    where TElse : IExpr
{
    private readonly TElse _elseExpr = elseExpr;

    public void FormatSql(SqlBuildingBuffer buffer) => buffer.Append(_elseExpr);
}
