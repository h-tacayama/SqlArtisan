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
            buffer.Append(".");
        }

        buffer.Append(Name);
    }
}
