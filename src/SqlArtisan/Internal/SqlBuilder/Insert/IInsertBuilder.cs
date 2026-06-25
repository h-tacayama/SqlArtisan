namespace SqlArtisan.Internal;

/// <summary>The entry state of an <c>INSERT</c> statement: name the target table.</summary>
public interface IInsertBuilder
{
    /// <summary>Opens <c>INSERT INTO table</c> without an explicit column list; supply the data with <c>Set(...)</c> or positional <c>Values(...)</c>.</summary>
    /// <param name="table">The table to insert into.</param>
    /// <returns>The builder positioned to add the data via <c>Set(...)</c> or <c>Values(...)</c>.</returns>
    IInsertBuilderTable InsertInto(DbTableBase table);

    /// <summary>Opens <c>INSERT INTO table (col, ...)</c> with an explicit column list; supply the rows with <c>Values(...)</c> or a <c>SELECT</c>.</summary>
    /// <param name="table">The table to insert into.</param>
    /// <param name="columns">The target columns, emitted in parentheses after the table name.</param>
    /// <returns>The builder positioned to add rows via <c>Values(...)</c> or a <c>SELECT</c> source.</returns>
    IInsertBuilderColumns InsertInto(DbTableBase table, params DbColumn[] columns);
}
