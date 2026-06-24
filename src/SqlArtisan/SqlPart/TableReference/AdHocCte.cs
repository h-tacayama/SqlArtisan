namespace SqlArtisan;

/// <summary>
/// An ad-hoc CTE handle: names a common table expression inline, without
/// declaring a dedicated <see cref="CteSchemaBase"/> subclass. Bind a subquery
/// with <see cref="CteSchemaBase.As"/> and reference its columns ad-hoc with
/// <see cref="Column"/>.
/// </summary>
public sealed class AdHocCte(string name) : CteSchemaBase(name)
{
    /// <summary>Returns the named column of this CTE, qualified by its name.</summary>
    public DbColumn Column(string columnName) => new(SchemaName, columnName);
}
