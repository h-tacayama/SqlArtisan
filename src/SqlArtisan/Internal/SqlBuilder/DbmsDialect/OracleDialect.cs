namespace SqlArtisan;

internal sealed class OracleDialect : IDbmsDialect
{
    public string GetParameterName(int index) => $":{index}";
}
