namespace SqlArtisan.Internal;

internal sealed class OracleDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    public char ParameterMarker => ':';

    public string ExcludedName => throw new NotSupportedException(
        "Oracle does not support ON CONFLICT / ON DUPLICATE KEY UPDATE. Use MERGE instead.");
}
