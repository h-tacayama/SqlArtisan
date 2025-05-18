namespace SqlArtisan;

internal sealed class InsertValuesClause : SqlPart
{
    private readonly SqlExpression[] _values;

    private InsertValuesClause(SqlExpression[] values)
    {
        _values = values;
    }

    internal static InsertValuesClause Parse(object[] values) =>
        new(InsertValueResolver.Resolve(values));

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Values} ")
        .OpenParenthesis()
        .AppendCsv(_values)
        .CloseParenthesis();
}
