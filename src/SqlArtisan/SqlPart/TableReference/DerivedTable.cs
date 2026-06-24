namespace SqlArtisan;

/// <summary>
/// A general-purpose derived-table handle: names a <c>CROSS APPLY</c> /
/// <c>LATERAL</c> source (or any <c>FROM</c> / <c>JOIN</c> relation) without
/// declaring a typed schema subclass. Reference its columns ad-hoc via
/// <see cref="Column"/>.
/// </summary>
public sealed class DerivedTable : DerivedTableSchemaBase
{
    public DerivedTable(string name) : base(name)
    {
    }

    /// <summary>Returns the named column of this derived table, qualified by its alias.</summary>
    public DbColumn Column(string columnName) => new(SchemaName, columnName);
}
