namespace SqlArtisan;

internal abstract class SqlBuilderBase(SqlPart clause)
{
    private readonly List<SqlPart> _clauses = [clause];

    protected void AddPart(SqlPart part)
    {
        _clauses.Add(part);
    }

    protected SqlStatement BuildCore() =>
        new SqlBuildingBuffer()
        .AppendSpaceSeparated(_clauses)
        .ToSqlStatement();

    internal void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.AppendSpaceSeparated(_clauses);
}
