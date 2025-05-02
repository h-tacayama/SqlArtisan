namespace SqlArtisan;

public sealed class CaseElseExpression : SqlExpression
{
    private readonly SqlExpression _elseExpr;

    internal CaseElseExpression(SqlExpression elseExpr)
    {
        _elseExpr = elseExpr;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(_elseExpr);
}
