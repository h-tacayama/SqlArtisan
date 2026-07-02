namespace SqlArtisan.Internal;

internal sealed class CommonTableExpressions(CommonTableExpression[] ctes)
{
    private readonly CommonTableExpression[] _ctes = ctes;

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
}
