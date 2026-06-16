namespace SqlArtisan.Internal;

internal sealed class SqlServerDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    public char ParameterMarker => '@';

    public string ExcludedName => throw new NotSupportedException(
        "SQL Server does not support ON CONFLICT / ON DUPLICATE KEY UPDATE. Use MERGE instead.");
}
