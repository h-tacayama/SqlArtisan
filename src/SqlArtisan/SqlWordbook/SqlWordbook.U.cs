using static SqlArtisan.ExpressionResolver;

namespace SqlArtisan;

public static partial class SqlWordbook
{
    public static IUpdateBuilderUpdate Update(DbTableBase table) =>
        new UpdateBuilder(new UpdateClause(table));

    public static UpperFunction Upper(object source) =>
        new(Resolve(source));
}
