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
    /// Whether the column has a <c>DEFAULT</c> or is assigned by the engine (identity,
    /// auto-increment, generated) — what lets a <c>NOT NULL</c> column be omitted from
    /// an <c>INSERT</c>.
    /// </summary>
    public bool HasDefault { get; init; }

    /// <summary>
    /// Whether the column leads a full index, so a predicate on it alone can use that
    /// index; a non-leading column of a composite index records <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// <para>Left unset when the column leads only a partial index, whose predicate
    /// covering a query is not decidable from the catalog.</para>
    /// <para>Left unset, too, when an index expression names the column or a PostgreSQL
    /// index that is neither B-tree nor hash has it as a key, even beside a plain
    /// index: either may serve the wrapped or leading-wildcard predicate.</para>
    /// </remarks>
    public bool Indexed { get; init; }

    /// <summary>
    /// The column's type reduced to one coarse category. A type name the generator
    /// does not recognize leaves this unset, which reads as
    /// <see cref="DbTypeCategory.Unknown"/>.
    /// </summary>
    public DbTypeCategory TypeCategory { get; init; }
}
