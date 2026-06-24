using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class AdHocSchemaTests
{
    private readonly TestTable _t = new("t");
    private readonly TestTable _s = new("s");
    private readonly TestTable _a = new("a");

    [Fact]
    public void AdHocDerivedTable_SqlServer_CrossApplyWithColumn_CorrectSql()
    {
        AdHocDerivedTable x = new("x");

        SqlStatement sql =
            Select(_t.Name, x.Column("total"))
            .From(_t)
            .CrossApply(
                Select(Sum(_s.Code).As(x.Column("total")))
                    .From(_s)
                    .Where(_s.Code == _t.Code),
                x)
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name, \"x\".total ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("CROSS APPLY ");
        expected.Append("(");
        expected.Append("SELECT SUM(\"s\".code) \"total\" FROM test_table \"s\" WHERE \"s\".code = \"t\".code");
        expected.Append(") ");
        expected.Append("x");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void AdHocCte_WithColumn_CorrectSql()
    {
        AdHocCte cte = new("cte");

        SqlStatement sql =
            With(
                cte.As(
                    Select(
                        _a.Code.As(cte.Column("c")),
                        _a.Name.As(cte.Column("n")))
                    .From(_a)
                    .Where(_a.Code == 1)))
            .Select(cte.Column("c"), cte.Column("n"))
                .From(cte)
            .Build();

        StringBuilder expected = new();
        expected.Append("WITH cte AS ");
        expected.Append("(");
        expected.Append("SELECT \"a\".code \"c\", \"a\".name \"n\" FROM test_table \"a\" WHERE \"a\".code = :0");
        expected.Append(") ");
        expected.Append("SELECT \"cte\".c, \"cte\".n ");
        expected.Append("FROM cte");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
