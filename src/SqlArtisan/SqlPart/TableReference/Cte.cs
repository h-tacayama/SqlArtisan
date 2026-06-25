using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// Names a common table expression inline, without declaring a dedicated
/// <see cref="CteSchemaBase"/> subclass. Bind a subquery with
/// <see cref="CteSchemaBase.As"/>; its columns are referenced by name through
/// <see cref="Column(string)"/>.
/// </summary>
public sealed class Cte(string name) : CteSchemaBase(name), IColumnAccessor
{
    /// <summary>Returns the named column of this CTE, qualified by its name.</summary>
    public DbColumn Column(string columnName) => new(SchemaName, columnName);

    /// <summary>Returns this CTE's column for <paramref name="sourceColumn"/> — its column name, qualified by this name. Use when the subquery projects the column unaliased.</summary>
    public DbColumn Column(DbColumn sourceColumn) => new(SchemaName, sourceColumn.Name);

    /// <summary>Returns this CTE's column for <paramref name="alias"/> — a SELECT-list <c>.As(...)</c> — qualified by this name.</summary>
    public DbColumn Column(ExpressionAlias alias) => new(SchemaName, alias.Alias);
}
