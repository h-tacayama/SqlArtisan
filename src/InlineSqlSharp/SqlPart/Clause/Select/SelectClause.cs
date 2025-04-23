namespace InlineSqlSharp;

internal sealed class SelectClause : AbstractSqlPart
{
    private readonly Hints? _hints;
    private readonly Distinct? _distinct;
    private readonly SelectItem[] _selectList;

    private SelectClause(
        Hints? hints,
        Distinct? distinct,
        SelectItem[] selectItems)
    {
        _hints = hints;
        _distinct = distinct;
        _selectList = selectItems;
    }

    internal static SelectClause Parse(
        Hints? hints,
        Distinct? distinct,
        object[] items) => new(
            hints,
            distinct,
            SelectItemResolver.Resolve(items));

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.SELECT)
        .AppendSpaceIfNotNull(_hints)
        .AppendSpaceIfNotNull(_distinct)
        .AppendSelectList(_selectList);
}
