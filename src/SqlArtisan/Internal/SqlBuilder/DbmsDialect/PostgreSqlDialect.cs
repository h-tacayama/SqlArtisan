namespace SqlArtisan;

internal sealed class PostgreSqlDialect : IDbmsDialect
{
    public string AliasQuote => "\"";

    public string ParameterMarker => ":";
}
