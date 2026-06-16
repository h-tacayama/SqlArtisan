using System.Data;
using SqlArtisan.Internal;

namespace SqlArtisan;

public static class IBulkInsertExtensions
{
    public static IReadOnlyList<SqlStatement> BuildBatches(
        this IBulkInsert bulkInsert,
        IDbConnection cnn)
    {
        Dbms dbms = DbmsResolver.Resolve(cnn);
        return bulkInsert.BuildBatches(dbms);
    }
}
