namespace InlineSqlSharp;

internal sealed class SelectClause : AbstractSqlPart
{
    private readonly Hints? _hints;
    private readonly Distinct? _distinct;
    private readonly AbstractSqlPart[] _selectItems;

    private SelectClause(
        Hints? hints,
        Distinct? distinct,
        AbstractSqlPart[] selectItems)
    {
        _hints = hints;
        _distinct = distinct;
        _selectItems = selectItems;
    }

    internal static SelectClause Parse(
        Hints? hints,
        Distinct? distinct,
        object[] selectItems) => new(
            hints,
            distinct,
            SelectItemResolver.Resolve(selectItems));

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.SELECT)
        .AppendSpaceIfNotNull(_hints)
        .AppendSpaceIfNotNull(_distinct)
        .AppendSelectItems(_selectItems);
}
