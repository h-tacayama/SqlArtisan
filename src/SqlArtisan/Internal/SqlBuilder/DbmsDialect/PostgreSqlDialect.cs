namespace SqlArtisan.Internal;

internal sealed class PostgreSqlDialect : IDbmsDialect
{
    public Dbms Dbms => Dbms.PostgreSql;

    public char AliasQuote => '"';

    public char ParameterMarker => ':';
}
