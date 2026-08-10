using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// Starts an <c>INSERT INTO table</c> statement with no column list.
    /// Continue with <c>.Values(...)</c> to supply the rows, which must line up
    /// with the table's column order.
    /// </summary>
    /// <param name="table">The target table.</param>
    /// <returns>An insert builder awaiting the values to insert.</returns>
    public static IInsertBuilderTable InsertInto(DbTableBase table) =>
        new InsertBuilder(table, 0, new InsertIntoClause(table));

    /// <summary>
    /// Starts an <c>INSERT INTO table (c1, c2)</c> statement naming
    /// <paramref name="columns"/> explicitly. Continue with <c>.Values(...)</c>
    /// (or <c>.Select(...)</c>) to supply rows matching the listed columns.
    /// </summary>
    /// <param name="table">The target table.</param>
    /// <param name="columns">The columns to insert into, emitted as a
    /// parenthesized list after the table.</param>
    /// <returns>An insert builder awaiting the values for the named columns.</returns>
    public static IInsertBuilderColumnsOutput InsertInto(DbTableBase table, params DbColumn[] columns) =>
        new InsertBuilder(table, columns.Length, new InsertIntoClause(table, columns));

    /// <summary>
    /// Starts an <c>INSERT IGNORE INTO table</c> statement (MySQL): rows whose
    /// insertion would raise an error — a duplicate key, and also FK violations or
    /// out-of-range values — are skipped rather than aborting the statement.
    /// Continue with <c>.Values(...)</c> to supply the rows.
    /// </summary>
    /// <param name="table">The target table.</param>
    /// <returns>An insert builder awaiting the values to insert.</returns>
    /// <remarks>MySQL syntax. On PostgreSQL/SQLite express the do-nothing UPSERT
    /// with <c>InsertInto(...).Values(...).OnConflict().DoNothing()</c> instead.</remarks>
    public static IInsertIgnoreBuilderTable InsertIgnoreInto(DbTableBase table) =>
        new InsertBuilder(table, 0, new InsertIgnoreIntoClause(table));

    /// <summary>
    /// Starts an <c>INSERT IGNORE INTO table (c1, c2)</c> statement (MySQL) naming
    /// <paramref name="columns"/> explicitly; error-raising rows are skipped rather
    /// than aborting the statement. Continue with <c>.Values(...)</c> (or
    /// <c>.Select(...)</c>) to supply rows matching the listed columns.
    /// </summary>
    /// <param name="table">The target table.</param>
    /// <param name="columns">The columns to insert into, emitted as a
    /// parenthesized list after the table.</param>
    /// <returns>An insert builder awaiting the values for the named columns.</returns>
    /// <remarks>MySQL syntax. On PostgreSQL/SQLite express the do-nothing UPSERT
    /// with <c>InsertInto(...).Values(...).OnConflict().DoNothing()</c> instead.</remarks>
    public static IInsertIgnoreBuilderColumns InsertIgnoreInto(DbTableBase table, params DbColumn[] columns) =>
        new InsertBuilder(table, columns.Length, new InsertIgnoreIntoClause(table, columns));

    /// <summary>
    /// References <paramref name="column"/> of the <c>INSERTED</c> pseudo-table in
    /// a SQL Server <c>OUTPUT</c> clause — the row's post-image after an
    /// <c>INSERT</c> or <c>UPDATE</c>. Renders as <c>INSERTED.col</c>.
    /// </summary>
    /// <param name="column">The target-table column whose inserted value to read.</param>
    /// <returns>An <c>INSERTED.col</c> reference.</returns>
    /// <remarks>SQL Server syntax, valid only inside <c>Output(...)</c>.</remarks>
    public static InsertedColumn Inserted(DbColumn column) => new(column);

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

    /// <summary>
    /// The <c>INTERVAL <paramref name="quantity"/> unit</c> expression
    /// (<paramref name="quantity"/> bound as a parameter, the unit emitted as a
    /// bare keyword).
    /// </summary>
    /// <param name="quantity">The number of units, bound as a parameter.</param>
    /// <param name="unit">The date/time unit.</param>
    /// <returns>An <see cref="IntervalExpression"/> emitting <c>INTERVAL :n unit</c>.</returns>
    /// <remarks>
    /// MySQL's idiomatic form — the quantity is bound rather than an inline
    /// literal. <see cref="IntervalLiteral(string, IntervalField)"/> is the
    /// Oracle/PostgreSQL spelling; MySQL's own grammar happens to accept that
    /// spelling too, but this bound form is preferred there.
    /// </remarks>
    public static IntervalExpression Interval(object quantity, DateTimePart unit) =>
        new(Resolve(quantity), unit);

    /// <summary>
    /// The <c>INTERVAL '<paramref name="text"/>'</c> literal, with its unit(s)
    /// embedded in the text itself (e.g. <c>"30 days"</c>).
    /// </summary>
    /// <param name="text">The interval literal text, emitted inline between
    /// single quotes (its own quotes doubled).</param>
    /// <returns>An <see cref="IntervalLiteralExpression"/> emitting <c>INTERVAL 'text'</c>.</returns>
    /// <remarks>PostgreSQL syntax.</remarks>
    public static IntervalLiteralExpression IntervalLiteral(string text) => new(text);

    /// <summary>
    /// The <c>INTERVAL '<paramref name="value"/>' field</c> literal, where
    /// <paramref name="field"/> is <see cref="Year(int?)"/>, <see cref="Month(int?)"/>,
    /// <see cref="Day(int?)"/>, <see cref="Hour(int?)"/>, <see cref="Minute(int?)"/>,
    /// or <see cref="Second()"/>.
    /// </summary>
    /// <param name="value">The interval literal value, emitted inline between
    /// single quotes (its own quotes doubled).</param>
    /// <param name="field">The field the literal is expressed in.</param>
    /// <returns>An <see cref="IntervalLiteralExpression"/> emitting
    /// <c>INTERVAL 'value' field</c>.</returns>
    /// <remarks>
    /// Oracle/PostgreSQL syntax; MySQL's own grammar happens to accept this
    /// exact spelling too, but prefer
    /// <see cref="Interval(object, DateTimePart)"/> there for a bound quantity.
    /// On Oracle, <see cref="Numtoyminterval(object, DateTimePart)"/> and
    /// <see cref="Numtodsinterval(object, DateTimePart)"/> are the bound-quantity
    /// counterparts to this literal form.
    /// </remarks>
    public static IntervalLiteralExpression IntervalLiteral(string value, IntervalField field) =>
        new(value, field);

    /// <summary>
    /// The <c>INTERVAL '<paramref name="value"/>' leadingField TO trailingField</c>
    /// literal — a range of fields (e.g. <c>YEAR TO MONTH</c>, <c>DAY TO SECOND</c>).
    /// </summary>
    /// <param name="value">The interval literal value, emitted inline between
    /// single quotes (its own quotes doubled).</param>
    /// <param name="leadingField">The most significant field — <see cref="Year(int?)"/>,
    /// <see cref="Day(int?)"/>, <see cref="Hour(int?)"/>, or <see cref="Minute(int?)"/>.</param>
    /// <param name="trailingField">The least significant field — <see cref="ToMonth"/>,
    /// <see cref="ToHour"/>, <see cref="ToMinute"/>, or <see cref="ToSecond(int?)"/>.</param>
    /// <returns>An <see cref="IntervalLiteralExpression"/> emitting
    /// <c>INTERVAL 'value' leadingField TO trailingField</c>.</returns>
    /// <exception cref="ArgumentException"><paramref name="leadingField"/> and
    /// <paramref name="trailingField"/> are not one of the seven valid Oracle
    /// pairings, or <paramref name="trailingField"/> carries a precision without
    /// being <see cref="ToSecond(int?)"/> — Oracle attaches a trailing precision
    /// to <c>SECOND</c> alone.</exception>
    /// <remarks>
    /// Oracle/PostgreSQL syntax. On Oracle, <paramref name="value"/> always
    /// renders as an inline literal — for a bound quantity there instead, use
    /// <see cref="Numtoyminterval(object, DateTimePart)"/> (<c>YEAR TO MONTH</c>)
    /// or <see cref="Numtodsinterval(object, DateTimePart)"/> (<c>DAY TO SECOND</c>).
    /// </remarks>
    public static IntervalLiteralExpression IntervalLiteral(
        string value,
        IntervalField leadingField,
        IntervalField trailingField) => new(value, leadingField, trailingField);
}
