using SqlArtisan.Internal;

namespace SqlArtisan;

public sealed class DbColumn(string tableAlias, string columnName) : SqlExpression
{
    private readonly string _tableAlias = tableAlias;
    private readonly string _columnName = columnName;

    internal override void Format(SqlBuildingBuffer buffer)
    {
        if (!string.IsNullOrEmpty(_tableAlias))
        {
            buffer.EncloseInDoubleQuotes(_tableAlias);
            buffer.Append(".");
        }

        buffer.Append(_columnName);
    }
}
