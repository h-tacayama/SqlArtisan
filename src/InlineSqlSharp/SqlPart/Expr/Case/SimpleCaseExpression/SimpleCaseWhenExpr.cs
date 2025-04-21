namespace InlineSqlSharp;

public sealed class SimpleCaseWhenExpr : AbstractSqlPart
{
    private readonly AbstractExpr _whenExpr;

    internal SimpleCaseWhenExpr(AbstractExpr whenExpr)
    {
        _whenExpr = whenExpr;
    }

    public SimpleCaseWhenClause THEN(object thenExpr) =>
        new(this, new CaseThenExpr(ExprResolver.Resolve(thenExpr)));

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.Append(_whenExpr);
}
