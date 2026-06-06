namespace SqlArtisan.Internal;

internal abstract class SqlBuilderBase(SqlPart[] rootParts)
{
    private readonly List<SqlPart> _parts = [.. rootParts];

    protected internal void AddPart(SqlPart part) => _parts.Add(part);

    internal SqlStatement BuildWithPart(SqlPart extraPart, Dbms dbms)
    {
        _parts.Add(extraPart);
        try
        {
            return BuildCore(dbms);
        }
        finally
        {
            _parts.RemoveAt(_parts.Count - 1);
        }
    }

    internal SqlStatement BuildWithPart(SqlPart extraPart) =>
        BuildWithPart(extraPart, SqlArtisanConfig.DefaultDbms);

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
