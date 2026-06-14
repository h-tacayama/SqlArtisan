using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

// #89 MERGE (Oracle / SQL Server) — neutral fluent API. The captured dialect
// divergence is the trailing semicolon: SQL Server requires it, Oracle forbids
// it. Both build from the same MergeBuilder; only Build(Dbms) differs.
public class MergeTests
{
    private const string MergeBody =
        "MERGE INTO test_table \"t\" USING test_table \"s\" " +
        "ON (\"t\".code = \"s\".code) " +
        "WHEN MATCHED THEN UPDATE SET \"t\".name = \"s\".name " +
        "WHEN NOT MATCHED THEN INSERT (code, name) VALUES (\"s\".code, \"s\".name)";

    [Fact]
    public void Merge_Oracle_HasNoTrailingSemicolon()
    {
        TestTable t = new("t");
        TestTable s = new("s");
        TestTable c = new();

        SqlStatement sql =
            MergeInto(t)
            .Using(s)
            .On(t.Code == s.Code)
            .WhenMatchedThenUpdateSet(t.Name == s.Name)
            .WhenNotMatchedThenInsert(c.Code, c.Name)
            .Values(s.Code, s.Name)
            .Build(Dbms.Oracle);

        Assert.Equal(MergeBody, sql.Text);
    }

    [Fact]
    public void Merge_SqlServer_HasTrailingSemicolon()
    {
        TestTable t = new("t");
        TestTable s = new("s");
        TestTable c = new();

        SqlStatement sql =
            MergeInto(t)
            .Using(s)
            .On(t.Code == s.Code)
            .WhenMatchedThenUpdateSet(t.Name == s.Name)
            .WhenNotMatchedThenInsert(c.Code, c.Name)
            .Values(s.Code, s.Name)
            .Build(Dbms.SqlServer);

        Assert.Equal(MergeBody + ";", sql.Text);
    }
}
