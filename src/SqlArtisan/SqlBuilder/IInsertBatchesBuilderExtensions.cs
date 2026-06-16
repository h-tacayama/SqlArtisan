using System.Data;
using SqlArtisan.Internal;

namespace SqlArtisan;

public static class IInsertBatchesBuilderExtensions
{
    public static IReadOnlyList<SqlStatement> BuildBatches(
        this IInsertBatchesBuilder insertBatchesBuilder,
        IDbConnection cnn)
    {
        Dbms dbms = DbmsResolver.Resolve(cnn);
        return insertBatchesBuilder.BuildBatches(dbms);
    }
}
