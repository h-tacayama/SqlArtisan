namespace SqlArtisan;

/// <summary>
/// Names a derived table inline — a <c>CROSS APPLY</c> / <c>LATERAL</c> source
/// (or any <c>FROM</c> / <c>JOIN</c> relation) — without declaring a dedicated
/// schema class, and renders as that bare name. Its columns are referenced by
/// name through <see cref="Column"/>. For columns referenced repeatedly, subclass
/// <see cref="DerivedTableSchemaBase"/> and expose them as typed members instead.
/// </summary>
public sealed class DerivedTable(string name) : DerivedTableSchemaBase(name)
{
    /// <summary>Returns the named column of this derived table, qualified by its alias.</summary>
    public DbColumn Column(string columnName) => new(SchemaName, columnName);
}
