using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// Names a common table expression inline, without declaring a dedicated
/// <see cref="CteBase"/> subclass. Bind a subquery with
/// <see cref="CteBase.As"/>; its columns are referenced by name through
/// <see cref="Column(string)"/>.
/// </summary>
public sealed class Cte(string name) : CteBase(name), IColumnAccessor
{
    /// <summary>
    /// Returns the named column of this CTE, qualified by its name.
    /// </summary>
    /// <param name="columnName">The column name to qualify with this CTE's name.</param>
    /// <returns>A <see cref="DbColumn"/> qualified by this CTE's name.</returns>
    public DbColumn Column(string columnName) => new(this, columnName);

    /// <summary>
    /// Returns this CTE's column for <paramref name="sourceColumn"/> — its column name, qualified by this name. Use when the subquery projects the column unaliased.
    /// </summary>
    /// <param name="sourceColumn">The source column whose name is re-qualified with this CTE's name.</param>
    /// <returns>A <see cref="DbColumn"/> qualified by this CTE's name.</returns>
    public DbColumn Column(DbColumn sourceColumn) => new(this, sourceColumn.Name);

    /// <summary>
    /// Returns this CTE's column for <paramref name="expressionAlias"/> — a SELECT-list <c>.As(...)</c> — qualified by this name.
    /// </summary>
    /// <param name="expressionAlias">The SELECT-list <c>.As(...)</c> alias to qualify with this CTE's name.</param>
    /// <returns>A <see cref="DbColumn"/> qualified by this CTE's name.</returns>
    public DbColumn Column(ExpressionAlias expressionAlias) => new(this, expressionAlias.Name);
}
