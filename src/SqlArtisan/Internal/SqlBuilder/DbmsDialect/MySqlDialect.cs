namespace SqlArtisan.Internal;

internal sealed class MySqlDialect : IDbmsDialect
{
    public Dbms Dbms => Dbms.MySql;

    public char AliasQuote => '`';

    public char ParameterMarker => '?';
}
