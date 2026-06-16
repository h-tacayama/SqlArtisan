namespace SqlArtisan.Internal;

internal sealed class MySqlDialect : IDbmsDialect
{
    public char AliasQuote => '`';

    public char ParameterMarker => '?';

    // MySQL's prepared-statement protocol caps placeholders at 65535 (16-bit count).
    public int MaxParameters => 65535;

    public bool SupportsMultiRowValues => true;

    // MySQL 8.0.19+ references the proposed row through a row alias rather than
    // the deprecated VALUES() function. The builder emits `... AS new` so the
    // update clause can read it as `new.column`.
    public string ExcludedName => "new";
}
