namespace SqlArtisan;

public sealed class SubstrBFunction : AbstractExpr
{
    private readonly AbstractSqlPart _source;
    private readonly AbstractSqlPart _position;
    private readonly AbstractSqlPart? _length;

    internal SubstrBFunction(
        AbstractExpr source,
        AbstractExpr position,
        AbstractExpr? length = null)
    {
        _source = source;
        _position = position;
        _length = length;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.SubstrB)
        .OpenParenthesis()
        .Append(_source)
        .PrependComma(_position)
        .PrependCommaIfNotNull(_length)
        .CloseParenthesis();
}
