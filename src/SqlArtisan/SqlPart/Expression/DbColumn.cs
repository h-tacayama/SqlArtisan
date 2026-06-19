using SqlArtisan.Internal;

namespace SqlArtisan;

public sealed class DbColumn(string tableAlias, string columnName) : SqlExpression
{
    private readonly string _tableAlias = tableAlias;
    internal string Name => columnName;

    internal override void Format(SqlBuildingBuffer buffer)
    {
        if (!string.IsNullOrEmpty(_tableAlias))
        {
            buffer.EncloseInAliasQuotes(_tableAlias);
            buffer.Append('.');
        }

        buffer.Append(Name);
    }

    // Renders the bare column name with no table-alias qualifier. DML contexts
    // that name a target column — the INSERT column list, the ON CONFLICT
    // target, and SET / DO UPDATE SET left sides — must stay unqualified;
    // PostgreSQL rejects an alias-qualified column in those positions.
    internal void FormatUnqualified(SqlBuildingBuffer buffer) =>
        buffer.Append(Name);
}
