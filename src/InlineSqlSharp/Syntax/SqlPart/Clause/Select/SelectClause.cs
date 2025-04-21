namespace InlineSqlSharp;

internal sealed class SelectClause : AbstractSqlPart
{
    private readonly Hints _hints;
    private readonly AllOrDistinct _allOrDistinct;
    private readonly SelectItem[] _selectList;

    private SelectClause(
        Hints hints,
        AllOrDistinct allOrDistinct,
        SelectItem[] selectItems)
    {
        _hints = hints;
        _allOrDistinct = allOrDistinct;
        _selectList = selectItems;
    }

    internal static SelectClause Parse(
        Hints hints,
        AllOrDistinct allOrDistinct,
        object[] items) => new(
            hints,
            allOrDistinct,
            SelectItemResolver.Resolve(items));

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.SELECT)
        .AppendSpaceIf(_hints.IsSome, _hints)
        .AppendSpaceIf(_allOrDistinct.IsDistinct, _allOrDistinct)
        .AppendSelectList(_selectList);
}
