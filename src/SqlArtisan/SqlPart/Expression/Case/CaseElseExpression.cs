using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// A <c>CASE</c> expression's <c>ELSE</c> branch, from <c>Sql.Else(...)</c>.
/// </summary>
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
