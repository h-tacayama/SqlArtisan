namespace InlineSqlSharp;

internal sealed class SelectClause(
    Hints hints,
    AllOrDistinct allOrDistinct,
    IExprOrAlias[] selectList) : ISqlElement
{
    private readonly Hints _hints = hints;
    private readonly AllOrDistinct _allOrDistinct = allOrDistinct;
    private readonly IExprOrAlias[] _selectList = selectList;

    public void FormatSql(SqlBuildingBuffer buffer) => buffer
        .AppendSpace(Keywords.SELECT)
        .AppendSpaceIf(_hints.IsSome, _hints)
        .AppendSpaceIf(_allOrDistinct.IsDistinct, _allOrDistinct)
        .AppendSelectList(_selectList);
}
