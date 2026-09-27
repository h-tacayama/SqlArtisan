namespace SqlArtisan.Internal;

/// <summary>
/// The entry state of an <c>INSERT</c> statement: name the target table.
/// </summary>
public interface IInsertBuilder
{
    // No InsertIgnoreInto: only the WITH states open this interface, and INSERT
    // IGNORE is MySQL's alone while MySQL's INSERT takes no leading WITH (#569).

    /// <summary>
    /// Opens <c>INSERT INTO table</c> without an explicit column list; supply the data with
    /// <c>Set(...)</c> or positional <c>Values(...)</c>.
    /// </summary>
    /// <param name="table">The table to insert into.</param>
    /// <returns>The builder positioned to add the data via <c>Set(...)</c> or <c>Values(...)</c>.</returns>
    IInsertBuilderTable InsertInto(DbTableBase table);

    /// <summary>
    /// Opens <c>INSERT INTO table (col, ...)</c> with an explicit column list; supply the rows with
    /// <c>Values(...)</c> or a <c>SELECT</c>.
    /// </summary>
    /// <param name="table">The table to insert into.</param>
    /// <param name="columns">The target columns, emitted in parentheses after the table name.</param>
    /// <returns>The builder positioned to add <c>OUTPUT</c>, then rows via <c>Values(...)</c> or a <c>SELECT</c> source, optionally led by its own <c>WITH</c>.</returns>
    IInsertBuilderColumnsOutput InsertInto(DbTableBase table, params DbColumn[] columns);
}
