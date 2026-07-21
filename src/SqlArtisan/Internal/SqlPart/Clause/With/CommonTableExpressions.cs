namespace SqlArtisan.Internal;

internal sealed class CommonTableExpressions
{
    private readonly CommonTableExpression[] _ctes;

    internal CommonTableExpressions(CommonTableExpression[] ctes)
    {
        CollectionGuard.ThrowIfEmpty(
            ctes,
            "WITH requires at least one common table expression.");

        _ctes = ctes;
    }

    internal void Format(SqlBuildingBuffer buffer, string withKeyword)
    {
        buffer.Append(withKeyword).AppendSpace();

        for (int i = 0; i < _ctes.Length; i++)
        {
            if (i > 0)
            {
                buffer.Append(", ");
            }

            _ctes[i].Format(buffer);
        }
    }

    internal void Format(SqlBuildingBuffer buffer, string withKeyword, string[][] columnNames)
    {
        buffer.Append(withKeyword).AppendSpace();

        for (int i = 0; i < _ctes.Length; i++)
        {
            if (i > 0)
            {
                buffer.Append(", ");
            }

            _ctes[i].Format(buffer, columnNames[i]);
        }
    }
}
