namespace SqlArtisan.Internal;

internal abstract class SqlBuilderBase(SqlPart part)
{
    private readonly List<SqlPart> _parts = [part];

    protected void AddPart(SqlPart part)
    {
        _parts.Add(part);
    }

    protected SqlStatement BuildCore(Dbms dbms)
    {
        IDbmsDialect dialect = DbmsDialectFactory.Create(dbms);
        using SqlBuildingBuffer buffer = new(dialect);
        return buffer
            .AppendSpaceSeparated(_parts)
            .ToSqlStatement();
    }

    internal void FormatCore(SqlBuildingBuffer buffer) =>
        buffer.AppendSpaceSeparated(_parts);
}
