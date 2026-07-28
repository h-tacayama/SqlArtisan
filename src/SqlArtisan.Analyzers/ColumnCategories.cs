namespace SqlArtisan.Analyzers;

// The generator reduces every catalog type name to one of these and writes it as
// the ColumnType argument; the analyzer only ever sees the category. Matched by
// string, never by a shared type (ADR 0009) — TableClassGen holds the other copy,
// and ColumnCategoryParityTests keeps the two spellings identical.
internal static class ColumnCategories
{
    public const string Text = "text";

    public const string Numeric = "numeric";

    public const string Temporal = "temporal";

    public const string Binary = "binary";

    public const string Boolean = "boolean";
}
