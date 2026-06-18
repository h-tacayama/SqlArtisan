namespace SqlArtisan.Internal;

internal sealed class SqliteDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    public char ParameterMarker => ':';

    public string ExcludedName => "excluded";

    public string StatementTerminator => "";
}
