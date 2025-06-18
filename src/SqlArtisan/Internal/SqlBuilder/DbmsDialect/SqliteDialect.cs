namespace SqlArtisan;

internal sealed class SqliteDialect : IDbmsDialect
{
    public string AliasQuote => "\"";

    public string ParameterMarker => ":";
}
