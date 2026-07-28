namespace SqlArtisan.Analyzers;

// The core's DbColumnType member names, which the generator writes symbolically
// and this reads back by name (ADR 0009 — no reference in either direction).
// SchemaMetadataParityTests keeps the two lists identical.
internal static class ColumnCategories
{
    public const string Unknown = "Unknown";

    public const string Text = "Text";

    public const string Numeric = "Numeric";

    public const string Temporal = "Temporal";

    public const string Binary = "Binary";

    public const string Boolean = "Boolean";
}
