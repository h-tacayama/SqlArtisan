namespace SqlArtisan;

/// <summary>
/// A general-purpose CTE handle: names a common table expression without
/// declaring a typed schema subclass. Bind a subquery with
/// <see cref="CteSchemaBase.As"/> and reference its columns ad-hoc via
/// <see cref="Column"/>.
/// </summary>
public sealed class Cte : CteSchemaBase
{
    public Cte(string name) : base(name)
    {
    }

    /// <summary>Returns the named column of this CTE, qualified by its name.</summary>
    public DbColumn Column(string columnName) => new(SchemaName, columnName);
}
