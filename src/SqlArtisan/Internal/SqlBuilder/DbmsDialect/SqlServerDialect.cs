namespace SqlArtisan.Internal;

internal sealed class SqlServerDialect : IDbmsDialect
{
    public Dbms Dbms => Dbms.SqlServer;

    public char AliasQuote => '"';

    public char ParameterMarker => '@';
}
