namespace SqlArtisan.Internal;

internal sealed class SqlServerDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    public char ParameterMarker => '@';

    public string CeilFunctionName => Keywords.Ceiling;
}
