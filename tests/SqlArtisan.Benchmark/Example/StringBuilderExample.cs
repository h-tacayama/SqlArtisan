using System.Text;
using Dapper;

namespace SqlArtisan.Benchmark;

public static class StringBuilderExample
{
    public static void Do()
    {
        StringBuilder query = new();
        query.Append("SELECT");
        query.Append("a.Id, ");
        query.Append("COUNT(*) AS Count ");
        query.Append("FROM ");
        query.Append("Authors a ");
        query.Append("JOIN ");
        query.Append("Books b ");
        query.Append("ON ");
        query.Append("a.Id = b.AuthorId ");
        query.Append("WHERE ");
        query.Append("b.Rating > @0 ");
        query.Append("AND b.Rating <= @1 ");
        query.Append("GROUP BY ");
        query.Append("a.Id ");
        query.Append("ORDER BY ");
        query.Append("a.Id");

        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("@0", 2.5);
        parameters.Add("@1", 5);
    }
}
