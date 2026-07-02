namespace SqlArtisan.Internal;

/// <summary>
/// The SQLite FTS5 <c>table MATCH pattern</c> predicate, matching rows of the
/// FTS table against the pattern. The table renders as its bare name, qualified
/// by its alias when one is declared (<c>"a".tbl MATCH ...</c>).
/// </summary>
public sealed class MatchCondition : SqlCondition
{
    private readonly DbTableBase _table;
    private readonly SqlExpression _pattern;

    internal MatchCondition(DbTableBase table, SqlExpression pattern)
    {
        _table = table;
        _pattern = pattern;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        _table.FormatAsMatchTarget(buffer);

        buffer
            .Append($" {Keywords.Match} ")
            .Append(_pattern);
    }
}
