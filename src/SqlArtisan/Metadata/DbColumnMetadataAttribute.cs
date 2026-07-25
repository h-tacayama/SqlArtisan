namespace SqlArtisan;

/// <summary>
/// Records what the schema says about the column a <see cref="DbColumn"/> property
/// exposes. SqlArtisan.TableClassGen emits it; the analyzer reads it at compile time
/// for schema-aware diagnostics. Leave a property unset when the fact is unknown —
/// an unset property, like a missing attribute, produces no diagnostics.
/// </summary>
/// <remarks>
/// Compile-time metadata only: nothing in SqlArtisan reads it at run time, so the
/// library stays reflection-free and AOT/trimming-safe.
/// </remarks>
// Named properties, not constructor parameters: "unknown" has to be representable,
// a positional parameter is mandatory, and `bool?` is rejected as an attribute
// argument type (CS0655). An unwritten argument is absent from NamedArguments.
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class DbColumnMetadataAttribute : Attribute
{
    /// <summary>Whether the column accepts <c>NULL</c>.</summary>
    public bool Nullable { get; init; }

    /// <summary>
    /// Whether an <c>INSERT</c> may omit the column — it has a <c>DEFAULT</c>, or the
    /// engine assigns it (identity, auto-increment, generated).
    /// </summary>
    public bool HasDefault { get; init; }
}
