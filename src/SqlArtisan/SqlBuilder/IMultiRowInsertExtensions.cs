using System.Data;
using SqlArtisan.Internal;

namespace SqlArtisan;

public static class IMultiRowInsertExtensions
{
    public static IReadOnlyList<SqlStatement> BuildBatches(
        this IMultiRowInsert multiRowInsert,
        IDbConnection cnn)
    {
        Dbms dbms = DbmsResolver.Resolve(cnn);
        return multiRowInsert.BuildBatches(dbms);
    }
}
