namespace SqlArtisan;

internal abstract class SqlBuilderBase(SqlPart part)
{
    private readonly List<SqlPart> _parts = [part];

    protected void AddPart(SqlPart part)
    {
        _parts.Add(part);
    }

    protected SqlStatement BuildCore()
    {
        using SqlBuildingBuffer buffer = new();
        return buffer
            .AppendSpaceSeparated(_parts)
            .ToSqlStatement();
    }

    internal void Format(SqlBuildingBuffer buffer) =>
        buffer.AppendSpaceSeparated(_parts);
}
