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
    public DbColumn Column(string columnName) => new(Alias, columnName);

    /// <summary>Returns this CTE's column for <paramref name="sourceColumn"/> — its column name, qualified by this name. Use when the subquery projects the column unaliased.</summary>
    public DbColumn Column(DbColumn sourceColumn) => new(Alias, sourceColumn.Name);

    /// <summary>Returns this CTE's column for <paramref name="expressionAlias"/> — a SELECT-list <c>.As(...)</c> — qualified by this name.</summary>
    public DbColumn Column(ExpressionAlias expressionAlias) => new(Alias, expressionAlias.Name);
}
