namespace SqlArtisan.Internal;

internal sealed class MySqlDialect : IDbmsDialect
{
    public string AliasQuote => "`";

    public string ParameterMarker => "?";
}
