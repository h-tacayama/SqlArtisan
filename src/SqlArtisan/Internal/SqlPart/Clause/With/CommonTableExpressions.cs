namespace SqlArtisan.Internal;

internal sealed class CommonTableExpressions(CommonTableExpression[] ctes)
{
    private readonly CommonTableExpression[] _ctes = ctes;

    internal void Format(SqlBuildingBuffer buffer, string withKeyword)
    {
        SqlPartAgent[] agents = new SqlPartAgent[_ctes.Length];
        for (int i = 0; i < _ctes.Length; i++)
        {
            agents[i] = new(_ctes[i].Format);
        }

        buffer.Append(withKeyword)
            .AppendSpace()
            .AppendCsv(agents);
    }
}
