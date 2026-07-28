namespace SqlArtisan.Analyzers;

// Mirrors the core's DbTypeCategory by member name (ADR 0009). A separate list
// rather than that file linked in: one free to lag the core is what lets a
// category this cannot name parse to nothing, and stay silent instead of
// judging a comparison it cannot reason about.
internal enum TypeCategory
{
    Unknown = 0,

    Text,

    Numeric,

    Temporal,

    Binary,

    Boolean,
}
