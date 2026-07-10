using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// Names a table inline, without declaring a dedicated <see cref="DbTableBase"/>
/// subclass; its columns are referenced by name through <see cref="Column(string)"/>,
/// qualified by the table alias when one is given. For columns referenced
/// repeatedly — or to get IntelliSense on column names — subclass
/// <see cref="DbTableBase"/> (or generate one with SqlArtisan.TableClassGen) instead.
/// </summary>
public sealed class DbTable(string tableName, string tableAlias = "")
    : DbTableBase(tableName, tableAlias), IColumnAccessor
{
    /// <summary>
    /// Returns the named column of this table, qualified by its alias (or unqualified when the table has no alias).
    /// </summary>
    /// <param name="columnName">The column name to qualify with this table's alias.</param>
    /// <returns>A <see cref="DbColumn"/> qualified by this table's alias.</returns>
    public DbColumn Column(string columnName) => new(this, columnName);

    /// <summary>
    /// Returns this table's column for <paramref name="sourceColumn"/> — its column name, qualified by this table's alias.
    /// </summary>
    /// <param name="sourceColumn">The source column whose name is re-qualified with this table's alias.</param>
    /// <returns>A <see cref="DbColumn"/> qualified by this table's alias.</returns>
    public DbColumn Column(DbColumn sourceColumn) => new(this, sourceColumn.Name);

    /// <summary>
    /// Returns this table's column for <paramref name="expressionAlias"/> — a SELECT-list <c>.As(...)</c> — qualified by this table's alias.
    /// </summary>
    /// <param name="expressionAlias">The SELECT-list <c>.As(...)</c> alias to qualify with this table's alias.</param>
    /// <returns>A <see cref="DbColumn"/> qualified by this table's alias.</returns>
    public DbColumn Column(ExpressionAlias expressionAlias) => new(this, expressionAlias.Name);
}
