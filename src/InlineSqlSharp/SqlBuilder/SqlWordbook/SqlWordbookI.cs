namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    public static IInsertBuilderInsertInto INSERT_INTO(AbstractTable table) =>
        new InsertBuilder(new InsertIntoClause(table));

    public static IInsertBuilderSelect INSERT_INTO(
        AbstractTable table,
        params IColumn[] columns) =>
        new InsertBuilder(new InsertSelectClause(table, columns));

    public static InstrFunction INSTR(
        CharacterExpr source,
        CharacterExpr substring) => new(source, substring);

    public static InstrFunction INSTR(
        CharacterExpr source,
        CharacterExpr substring,
        NumericExpr position) => new(source, substring, position);

    public static InstrFunction INSTR(
        CharacterExpr source,
        CharacterExpr substring,
        NumericExpr position,
        NumericExpr occurrence) => new(source, substring, position, occurrence);
}
