namespace SqlArtisan;

internal sealed class SqliteDialect : IDbmsDialect
{
    public string GetParameterName(int index) => $":{index}";
}
