namespace SqlArtisan.TableClassGen;

// Which columns lead a full index, which lead only a partial one, and which are
// named by some index expression. The last two only ever produce silence — an
// expression index exists precisely so the wrapped predicate can be written, and
// whether a partial index's predicate covers a query is an expression to
// interpret, which #266 rules out.
internal sealed class ColumnIndexInfo(
    IReadOnlyCollection<string> leadingColumns,
    IReadOnlyCollection<string> expressionTexts,
    IReadOnlyCollection<string> partialLeadingColumns,
    bool allUnknown = false)
{
    // For a catalog path that knows an index expression exists but cannot read its
    // text — Oracle's COLUMN_EXPRESSION is a LONG — so no column can be claimed.
    public static ColumnIndexInfo Unknown { get; } = new([], [], [], allUnknown: true);

    // Null where an index expression names the column: the tri-state's unknown,
    // which the emitter writes as an absent argument. A full-index lead beats a
    // partial one — the full index serves a bare predicate whatever the partial
    // one covers.
    public bool? IsIndexed(string columnName) =>
        allUnknown || MentionedByExpression(columnName) ? null
        : leadingColumns.Contains(columnName, StringComparer.Ordinal) ? true
        : partialLeadingColumns.Contains(columnName, StringComparer.Ordinal) ? null
        : false;

    // A whole-word scan of the expression text, never a parse: matching
    // UPPER(name) against PostgreSQL's stored upper((name)::text) is exactly the
    // interpretation #266 rules out, and over-matching costs only a warning.
    private bool MentionedByExpression(string columnName) =>
        expressionTexts.Any(text => ContainsIdentifier(text, columnName));

    private static bool ContainsIdentifier(string text, string identifier)
    {
        int start = 0;

        while (start <= text.Length - identifier.Length)
        {
            int at = text.IndexOf(identifier, start, StringComparison.OrdinalIgnoreCase);
            if (at < 0)
            {
                return false;
            }

            if (!IsIdentifierChar(text, at - 1) && !IsIdentifierChar(text, at + identifier.Length))
            {
                return true;
            }

            start = at + 1;
        }

        return false;
    }

    private static bool IsIdentifierChar(string text, int index) =>
        index >= 0
        && index < text.Length
        && (char.IsLetterOrDigit(text[index]) || text[index] == '_');
}
