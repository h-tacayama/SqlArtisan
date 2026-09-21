namespace SqlArtisan.Analyzers;

// Mirrors the core's DbTypeCategory by member name (ADR 0009), as a separate list:
// a category this cannot name parses to nothing and stays silent, instead of
// the rule judging a comparison it cannot reason about.
internal enum TypeCategory
{
    Unknown = 0,

    Text,

    Numeric,

    Temporal,

    Binary,

    Boolean,
}
