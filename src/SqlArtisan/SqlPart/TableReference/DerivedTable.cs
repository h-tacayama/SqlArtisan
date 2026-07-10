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
    /// <param name="name">The column name to qualify with this derived table's alias.</param>
    /// <returns>A <see cref="DbColumn"/> qualified by this derived table's alias.</returns>
    public DbColumn Column(string name) => new(this, name);

    /// <summary>
    /// Returns this derived table's column for <paramref name="source"/> — its column name, qualified by this alias. Use when the subquery projects the column unaliased.
    /// </summary>
    /// <param name="source">The source column whose name is re-qualified with this derived table's alias.</param>
    /// <returns>A <see cref="DbColumn"/> qualified by this derived table's alias.</returns>
    public DbColumn Column(DbColumn source) => new(this, source.Name);

    /// <summary>
    /// Returns this derived table's column for <paramref name="alias"/> — a SELECT-list <c>.As(...)</c> — qualified by this alias.
    /// </summary>
    /// <param name="alias">The SELECT-list <c>.As(...)</c> alias to qualify with this derived table's alias.</param>
    /// <returns>A <see cref="DbColumn"/> qualified by this derived table's alias.</returns>
    public DbColumn Column(ExpressionAlias alias) => new(this, alias.Name);
}
