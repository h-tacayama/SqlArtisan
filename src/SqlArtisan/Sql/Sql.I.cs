using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// Starts an <c>INSERT INTO table</c> statement with no column list.
    /// Continue with <c>.Values(...)</c> (or <c>.Select(...)</c>) to supply the
    /// rows, which must line up with the table's column order.
    /// </summary>
    /// <param name="table">The target table.</param>
    /// <returns>An insert builder awaiting the values to insert.</returns>
    public static IInsertBuilderTable InsertInto(DbTableBase table) =>
        new InsertBuilder(new InsertIntoClause(table));

    /// <summary>
    /// Starts an <c>INSERT INTO table (c1, c2)</c> statement naming
    /// <paramref name="columns"/> explicitly. Continue with <c>.Values(...)</c>
    /// (or <c>.Select(...)</c>) to supply rows matching the listed columns.
    /// </summary>
    /// <param name="table">The target table.</param>
    /// <param name="columns">The columns to insert into, emitted as a
    /// parenthesized list after the table.</param>
    /// <returns>An insert builder awaiting the values for the named columns.</returns>
    public static IInsertBuilderColumns InsertInto(DbTableBase table, params DbColumn[] columns) =>
        new InsertBuilder(new InsertIntoClause(table, columns));

    /// <summary>
    /// The <c>INSTR(<paramref name="source"/>, <paramref name="substring"/>)</c>
    /// function: the 1-based position of the first occurrence of
    /// <paramref name="substring"/> within <paramref name="source"/>, or 0 when
    /// not found.
    /// </summary>
    /// <param name="source">The string to search in.</param>
    /// <param name="substring">The substring to search for.</param>
    /// <returns>The INSTR construct.</returns>
    /// <remarks>MySQL, Oracle, and SQLite syntax; the 3- and 4-argument forms
    /// are Oracle-only.</remarks>
    public static InstrFunction Instr(object source, object substring) =>
        new(Resolve(source), Resolve(substring));

    /// <inheritdoc cref="Instr(object, object)"/>
    /// <param name="source">The string to search in.</param>
    /// <param name="substring">The substring to search for.</param>
    /// <param name="position">The 1-based position at which to start searching.</param>
    public static InstrFunction Instr(object source, object substring, object position) =>
        new(Resolve(source), Resolve(substring), Resolve(position));

    /// <inheritdoc cref="Instr(object, object)"/>
    /// <param name="source">The string to search in.</param>
    /// <param name="substring">The substring to search for.</param>
    /// <param name="position">The 1-based position at which to start searching.</param>
    /// <param name="occurrence">Which occurrence (1-based) to locate.</param>
    public static InstrFunction Instr(
        object source,
        object substring,
        object position,
        object occurrence) => new(
            Resolve(source),
            Resolve(substring),
            Resolve(position),
            Resolve(occurrence));
}
