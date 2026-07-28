namespace SqlArtisan;

/// <summary>
/// A column's type reduced to one coarse category, as
/// <see cref="DbColumnMetadataAttribute.TypeCategory"/> records it.
/// </summary>
/// <remarks>
/// No precision, length, or scale: comparing a <c>numeric(10,2)</c> column to an
/// <c>int</c> is not a mismatch, and carrying width would invite judgments about
/// values rather than types.
/// </remarks>
public enum DbTypeCategory
{
    /// <summary>
    /// The generator did not recognize the catalog's type name, so the column
    /// carries no claim about its type. This is the unwritten default.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Character data — <c>char</c>, <c>varchar</c>, <c>text</c>, <c>clob</c>.
    /// </summary>
    Text,

    /// <summary>
    /// Numbers, exact or approximate — <c>int</c>, <c>decimal</c>, <c>float</c>.
    /// </summary>
    Numeric,

    /// <summary>
    /// Dates, times, and intervals.
    /// </summary>
    Temporal,

    /// <summary>
    /// Raw bytes — <c>blob</c>, <c>bytea</c>, <c>varbinary</c>.
    /// </summary>
    Binary,

    /// <summary>
    /// A truth value.
    /// </summary>
    Boolean,
}
