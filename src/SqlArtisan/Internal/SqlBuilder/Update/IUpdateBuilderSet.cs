namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>UPDATE ... SET</c>: join other tables with <c>FROM</c>
/// (PostgreSQL / SQLite, and SQL Server when the target is re-listed), narrow
/// with <c>WHERE</c>, add <c>RETURNING</c>, or build.
/// </summary>
public interface IUpdateBuilderSet : ISqlBuilder, IReturning
{
    /// <summary>
    /// Appends <c>FROM table, ...</c> naming the tables the update draws from
    /// (PostgreSQL / SQLite <c>UPDATE ... FROM</c>). Re-list the target table
    /// here for the SQL Server form (its alias then leads the statement).
    /// </summary>
    /// <param name="tables">The tables to join into the update; re-listing the target selects the SQL Server spelling.</param>
    /// <returns>The builder positioned to join further tables, filter with <c>WHERE</c>, or build.</returns>
    IUpdateBuilderFrom From(params TableReference[] tables);

    /// <summary>
    /// Appends <c>WHERE condition</c> to restrict which rows are updated.
    /// </summary>
    /// <param name="condition">The row filter; literals it contains are auto-parameterized.</param>
    /// <returns>The builder positioned for <c>RETURNING</c> or build.</returns>
    IUpdateBuilderWhere Where(SqlCondition condition);
}
