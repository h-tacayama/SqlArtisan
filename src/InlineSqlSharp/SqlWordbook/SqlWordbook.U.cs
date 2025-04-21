using static InlineSqlSharp.ExprResolver;

namespace InlineSqlSharp;

public static partial class SqlWordbook
{
    public static IUpdateBuilderUpdate UPDATE(AbstractTable table) =>
        new UpdateBuilder(new UpdateClause(table));

    public static UpperFunction UPPER(object source) =>
        new(Resolve(source));
}
