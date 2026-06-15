namespace SqlArtisan.Internal;

internal sealed class OracleDialect : IDbmsDialect
{
    public char AliasQuote => '"';

    public char ParameterMarker => ':';

    public string ExcludedReference => throw new NotSupportedException(
        "Oracle does not support ON CONFLICT / ON DUPLICATE KEY UPDATE. Use MERGE instead.");
}
