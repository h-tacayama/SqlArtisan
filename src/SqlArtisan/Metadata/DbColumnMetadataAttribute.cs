namespace SqlArtisan;

/// <summary>
/// Records what the schema says about the column a <see cref="DbColumn"/> property
/// exposes. Leave a property unset when the fact is unknown — like a missing
/// attribute, it then carries no claim about the column.
/// </summary>
// Named properties, not constructor parameters: a positional parameter is mandatory
// and `bool?` is rejected as an attribute argument (CS0655), so unknown = unwritten.
// Compile-time only — nothing reads this at run time, keeping the core reflection-free.
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class DbColumnMetadataAttribute : Attribute
{
    /// <summary>
    /// Whether the column accepts <c>NULL</c>.
    /// </summary>
    public bool Nullable { get; init; }

    /// <summary>
    /// Whether an <c>INSERT</c> may omit the column — it has a <c>DEFAULT</c>, or the
    /// engine assigns it (identity, auto-increment, generated).
    /// </summary>
    public bool HasDefault { get; init; }
}
