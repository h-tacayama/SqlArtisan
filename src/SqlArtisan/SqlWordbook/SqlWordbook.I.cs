using static SqlArtisan.ExpressionResolver;

namespace SqlArtisan;

public static partial class SqlWordbook
{
    public static IInsertBuilderInsertInto InsertInto(
        DbTableBase table,
        params DbColumn[] columns) =>
        new InsertBuilder(new InsertIntoClause(table, columns));

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
