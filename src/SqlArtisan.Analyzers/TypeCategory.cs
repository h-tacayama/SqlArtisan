namespace SqlArtisan.Analyzers;

// Mirrors the core's DbTypeCategory by member name, never by a reference or by
// the underlying value (ADR 0009). A category this cannot name will not parse,
// so a core newer than the analyzer degrades to silence instead of a verdict
// about a category the rule cannot reason about.
internal enum TypeCategory
{
    Unknown = 0,

    Text,

    Numeric,

    Temporal,

    Binary,

    Boolean,
}
