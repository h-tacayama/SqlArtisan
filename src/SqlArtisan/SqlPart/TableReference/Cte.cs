namespace SqlArtisan;

/// <summary>
/// Names a common table expression inline, without declaring a dedicated
/// <see cref="CteSchemaBase"/> subclass. Bind a subquery with
/// <see cref="CteSchemaBase.As"/>; its columns are referenced by name through
/// <see cref="Column"/>.
/// </summary>
public sealed class Cte(string name) : CteSchemaBase(name)
{
    /// <summary>Returns the named column of this CTE, qualified by its name.</summary>
    public DbColumn Column(string columnName) => new(SchemaName, columnName);
}
