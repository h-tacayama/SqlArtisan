namespace SqlArtisan;

internal sealed class MySqlDialect : IDbmsDialect
{
    public string GetParameterName(int index) => $"?{index}";
}
