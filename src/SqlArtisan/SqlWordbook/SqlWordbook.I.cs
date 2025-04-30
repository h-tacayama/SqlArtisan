using static SqlArtisan.ExprResolver;

namespace SqlArtisan;

public static partial class SqlWordbook
{
    public static IInsertBuilderInsertInto InsertInto(AbstractTable table) =>
        new InsertBuilder(new InsertIntoClause(table));

    public static IInsertBuilderSelect InsertInto(
        AbstractTable table,
        params Column[] columns) =>
        new InsertBuilder(new InsertSelectClause(table, columns));

    public static InstrFunction Instr(
        object source,
        object substring) => new(
            Resolve(source),
            Resolve(substring));

    public static InstrFunction Instr(
        object source,
        object substring,
        object position) => new(
            Resolve(source),
            Resolve(substring),
            Resolve(position));

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
