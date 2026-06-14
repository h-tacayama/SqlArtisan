using SqlArtisan.Internal;

namespace SqlArtisan.Databases.MySql;

// MySQL's only UPSERT verb is ON DUPLICATE KEY UPDATE — a single extension, no
// DoUpdateSet/DoNothing branch. One method, no wrapper types.
public static partial class Sql
{
    public static IExtUpsertColumns InsertInto(DbTableBase table, params DbColumn[] columns) =>
        new ExtUpsertBuilder(new InsertBuilder(new InsertIntoClause(table, columns)));
}

public static class MySqlUpsertExtensions
{
    public static MySqlQuery OnDuplicateKeyUpdate(this IExtUpsertValues values, params DbColumn[] updateColumns) =>
        new(((ExtUpsertBuilder)values).Inner.OnDuplicateKeyUpdate(updateColumns));
}
