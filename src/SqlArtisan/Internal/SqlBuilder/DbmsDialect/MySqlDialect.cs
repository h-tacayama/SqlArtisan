namespace SqlArtisan.Internal;

internal sealed class MySqlDialect : IDbmsDialect
{
    public char AliasQuote => '`';

    public char ParameterMarker => '?';
}
