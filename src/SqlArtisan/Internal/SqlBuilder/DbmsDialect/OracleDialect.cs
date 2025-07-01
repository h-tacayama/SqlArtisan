namespace SqlArtisan.Internal;

internal sealed class OracleDialect : IDbmsDialect
{
    public string AliasQuote => "\"";

    public string ParameterMarker => ":";
}
