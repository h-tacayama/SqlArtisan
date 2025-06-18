namespace SqlArtisan;

internal sealed class SqlServerDialect : IDbmsDialect
{
    public string AliasQuote => "\"";

    public string ParameterMarker => "@";
}
