namespace SqlArtisan.Internal;

// Implemented by the ad-hoc relation handles (DerivedTable, Cte) to force them to
// expose the same Column(...) surface: adding an overload here breaks compilation
// of every implementer until it is added.
internal interface IColumnAccessor
{
    /// <summary>
    /// Returns the named column of this derived table, qualified by its alias.
    /// </summary>
    /// <param name="name">The column name to qualify with this derived table's alias.</param>
    /// <returns>A <see cref="DbColumn"/> qualified by this derived table's alias.</returns>
    DbColumn Column(string name);

    /// <summary>
    /// Returns this derived table's column for <paramref name="source"/> — its column name, qualified by this alias. Use when the subquery projects the column unaliased.
    /// </summary>
    /// <param name="source">The source column whose name is re-qualified with this derived table's alias.</param>
    /// <returns>A <see cref="DbColumn"/> qualified by this derived table's alias.</returns>
    DbColumn Column(DbColumn source);

    /// <summary>
    /// Returns this derived table's column for <paramref name="alias"/> — a SELECT-list <c>.As(...)</c> — qualified by this alias.
    /// </summary>
    /// <param name="alias">The SELECT-list <c>.As(...)</c> alias to qualify with this derived table's alias.</param>
    /// <returns>A <see cref="DbColumn"/> qualified by this derived table's alias.</returns>
    DbColumn Column(ExpressionAlias alias);
}
