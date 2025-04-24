namespace InlineSqlSharp;

internal abstract class AbstractSqlBuilder(AbstractSqlPart clause)
{
    private readonly List<AbstractSqlPart> _clauses = [clause];

    protected void AddPart(AbstractSqlPart part)
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
