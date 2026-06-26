using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// Names a derived table inline — a <c>CROSS APPLY</c> / <c>LATERAL</c> source
/// (or any <c>FROM</c> / <c>JOIN</c> relation) — without declaring a dedicated
/// subclass, and renders as that alias-quoted name (e.g. <c>"x"</c>). Its columns
/// are referenced by name through <see cref="Column(string)"/>. For columns referenced repeatedly,
/// subclass <see cref="DerivedTableBase"/> and expose them as typed members
/// instead.
/// </summary>
public sealed class DerivedTable(string name) : DerivedTableBase(name), IColumnAccessor
{
    /// <summary>
    /// Returns the named column of this derived table, qualified by its alias.
    /// </summary>
    /// <param name="columnName">The column name to qualify with this derived table's alias.</param>
    /// <returns>A <see cref="DbColumn"/> qualified by this derived table's alias.</returns>
    public DbColumn Column(string columnName) => new(_name, columnName);

    /// <summary>
    /// Returns this derived table's column for <paramref name="sourceColumn"/> — its column name, qualified by this alias. Use when the subquery projects the column unaliased.
    /// </summary>
    /// <param name="sourceColumn">The source column whose name is re-qualified with this derived table's alias.</param>
    /// <returns>A <see cref="DbColumn"/> qualified by this derived table's alias.</returns>
    public DbColumn Column(DbColumn sourceColumn) => new(_name, sourceColumn.Name);

    /// <summary>
    /// Returns this derived table's column for <paramref name="expressionAlias"/> — a SELECT-list <c>.As(...)</c> — qualified by this alias.
    /// </summary>
    /// <param name="expressionAlias">The SELECT-list <c>.As(...)</c> alias to qualify with this derived table's alias.</param>
    /// <returns>A <see cref="DbColumn"/> qualified by this derived table's alias.</returns>
    public DbColumn Column(ExpressionAlias expressionAlias) => new(_name, expressionAlias.Name);
}
