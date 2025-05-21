namespace SqlArtisan.Internal;

public sealed class CaseElseExpression : SqlExpression
{
    private readonly SqlExpression _elseExpr;

    internal CaseElseExpression(SqlExpression elseExpr)
    {
        _elseExpr = elseExpr;
    }

    internal override void Format(SqlBuildingBuffer buffer) =>
        _elseExpr.Format(buffer);
}
