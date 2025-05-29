namespace SqlArtisan;

internal sealed class PostgreSqlDialect : IDbmsDialect
{
    public string GetParameterName(int index) => $":{index}";
}
