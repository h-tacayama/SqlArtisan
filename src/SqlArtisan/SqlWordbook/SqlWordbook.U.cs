using static SqlArtisan.ExprResolver;

namespace SqlArtisan;

public static partial class SqlWordbook
{
    public static IUpdateBuilderUpdate Update(AbstractTable table) =>
        new UpdateBuilder(new UpdateClause(table));

    public static UpperFunction Upper(object source) =>
        new(Resolve(source));
}
