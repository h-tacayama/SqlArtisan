namespace SqlArtisan.TableClassGen;

// Which columns lead an index a bare predicate can use, which lead only one the
// catalog cannot decide (partial, or neither B-tree nor hash), and which an index
// expression names. The last two only ever produce silence, as #266 rules.
internal sealed class ColumnIndexInfo(
    IReadOnlyCollection<string> leadingColumns,
    IReadOnlyCollection<string> expressionTexts,
    IReadOnlyCollection<string> undecidedLeadingColumns,
    bool allUnknown = false)
{
    // For a catalog path that knows an index expression exists but cannot read its
    // text — Oracle's COLUMN_EXPRESSION is a LONG — so no column can be claimed.
    public static ColumnIndexInfo Unknown { get; } = new([], [], [], allUnknown: true);

    // A decided lead beats an undecided lead and a mention in a separate
    // expression index alike — it serves a bare predicate regardless of what
    // either of those covers.
    public bool? IsIndexed(string columnName) =>
        allUnknown ? null
        : leadingColumns.Contains(columnName, StringComparer.Ordinal) ? true
        : MentionedByExpression(columnName) ? null
        : undecidedLeadingColumns.Contains(columnName, StringComparer.Ordinal) ? null
        : false;

    // A whole-word scan of the expression text, never a parse: matching
    // UPPER(name) against PostgreSQL's stored upper((name)::text) is exactly the
    // interpretation #266 rules out. Over-matching can only turn false into unknown.
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
