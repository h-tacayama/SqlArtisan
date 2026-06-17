namespace SqlArtisan.Internal;

/// <summary>
/// The MySQL <c>SEPARATOR &lt;value&gt;</c> clause of <c>GROUP_CONCAT</c>. Use
/// <c>Sql.Separator(...)</c> to select MySQL's keyword form over SQLite's
/// positional separator argument. MySQL's grammar requires the separator to be
/// a string literal (a bind parameter is a syntax error), so the value is
/// emitted inline as a single-quote-escaped literal.
/// </summary>
public sealed class SeparatorClause : SqlPart
{
    private readonly string _separator;

    internal SeparatorClause(string separator)
    {
        _separator = separator
            ?? throw new ArgumentNullException(nameof(separator));
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Separator)
        .AppendSpace()
        .Append('\'')
        .Append(Escape(_separator))
        .Append('\'');

    // MySQL string literals treat both the single quote and (in the default SQL
    // mode) the backslash as special, so double each to emit a safe literal.
    private static string Escape(string value) =>
        value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("'", "''", StringComparison.Ordinal);
}
