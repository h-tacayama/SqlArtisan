namespace SqlArtisan;

public sealed class SumFunction : AbstractExpr
{
    private readonly DistinctKeyword? _distinct;
    private readonly AbstractSqlPart _expr;

    internal SumFunction(AbstractExpr expr)
    {
        _distinct = null;
        _expr = expr;
    }

    internal SumFunction(DistinctKeyword distinct, AbstractExpr expr)
    {
        _distinct = distinct;
        _expr = expr;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Sum)
        .OpenParenthesis()
        .AppendSpaceIfNotNull(_distinct)
        .Append(_expr)
        .CloseParenthesis();
}
