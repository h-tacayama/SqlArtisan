namespace SqlArtisan;

internal sealed class SqlServerDialect : IDbmsDialect
{
    public string GetParameterName(int index) => $"@{index}";
}
