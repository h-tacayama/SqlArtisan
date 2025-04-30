namespace SqlArtisan;

public sealed class CountFunction : AbstractExpr
{
    private readonly DistinctKeyword? _distinct;
    private readonly AbstractSqlPart _expr;

    internal CountFunction(AbstractExpr expr)
    {
        _distinct = null;
        _expr = expr;
    }

    internal CountFunction(DistinctKeyword distinct, AbstractExpr expr)
    {
        _distinct = distinct;
        _expr = expr;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Count)
        .OpenParenthesis()
        .AppendSpaceIfNotNull(_distinct)
        .Append(_expr)
        .CloseParenthesis();
}
