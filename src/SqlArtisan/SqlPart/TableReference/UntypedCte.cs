namespace SqlArtisan;

/// <summary>
/// An untyped CTE handle: names a common table expression without declaring a
/// typed <see cref="CteSchemaBase"/> subclass. Bind a subquery with
/// <see cref="CteSchemaBase.As"/>; its columns are referenced by name through
/// <see cref="Column"/> rather than as statically-typed members.
/// </summary>
public sealed class UntypedCte(string name) : CteSchemaBase(name)
{
    /// <summary>Returns the named column of this CTE, qualified by its name.</summary>
    public DbColumn Column(string columnName) => new(SchemaName, columnName);
}
