using static InlineSqlSharp.ExprResolver;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    public static IInsertBuilderInsertInto INSERT_INTO(AbstractTable table) =>
        new InsertBuilder(new InsertIntoClause(table));

    public static IInsertBuilderSelect INSERT_INTO(
        AbstractTable table,
        params Column[] columns) =>
        new InsertBuilder(new InsertSelectClause(table, columns));

    public static InstrFunction INSTR(
        object source,
        object substring) => new(
            Resolve(source),
            Resolve(substring));

    public static InstrFunction INSTR(
        object source,
        object substring,
        object position) => new(
            Resolve(source),
            Resolve(substring),
            Resolve(position));

    public static InstrFunction INSTR(
        object source,
        object substring,
        object position,
        object occurrence) => new(
            Resolve(source),
            Resolve(substring),
            Resolve(position),
            Resolve(occurrence));
}
