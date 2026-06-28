using System.Text;
using SqlArtisan.Internal;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class DerivedTableTests
{
    private readonly TestTable _t = new("t");
    private readonly TestTable _s = new("s");
    private readonly TestTable _a = new("a");

    [Fact]
    public void DerivedTable_SqlServer_CrossApplyWithColumn_CorrectSql()
    {
        DerivedTable x = new("x");

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
        expected.Append("SELECT SUM(\"s\".code) total FROM test_table \"s\" WHERE \"s\".code = \"t\".code");
        expected.Append(") ");
        expected.Append("\"x\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DerivedTable_SqlServer_CrossApplyTypedColumns_CorrectSql()
    {
        TestDerivedTable x = new("x");

        SqlStatement sql =
            Select(_t.Name, x.Total)
            .From(_t)
            .CrossApply(
                Select(Sum(_s.Code).As(x.Total))
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
        expected.Append("SELECT SUM(\"s\".code) total FROM test_table \"s\" WHERE \"s\".code = \"t\".code");
        expected.Append(") ");
        expected.Append("\"x\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Cte_WithColumn_CorrectSql()
    {
        Cte cte = new("cte");

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
        expected.Append("WITH \"cte\" AS ");
        expected.Append("(");
        expected.Append("SELECT \"a\".code c, \"a\".name n FROM test_table \"a\" WHERE \"a\".code = :0");
        expected.Append(") ");
        expected.Append("SELECT \"cte\".c, \"cte\".n ");
        expected.Append("FROM \"cte\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DerivedTable_PostgreSql_ColumnFromSourceColumn_CorrectSql()
    {
        DerivedTable x = new("x");

        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .JoinLateral(Select(_s.Code).From(_s), x)
            .On(_t.Code == x.Column(_s.Code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("JOIN LATERAL ");
        expected.Append("(SELECT \"s\".code FROM test_table \"s\") ");
        expected.Append("\"x\" ");
        expected.Append("ON ");
        expected.Append("\"t\".code = \"x\".code");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DerivedTable_SqlServer_ColumnFromAlias_CorrectSql()
    {
        DerivedTable x = new("x");
        ExpressionAlias total = Sum(_s.Code).As("total");

        SqlStatement sql =
            Select(_t.Name, x.Column(total))
            .From(_t)
            .CrossApply(Select(total).From(_s).Where(_s.Code == _t.Code), x)
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name, \"x\".total ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("CROSS APPLY ");
        expected.Append("(SELECT SUM(\"s\".code) \"total\" FROM test_table \"s\" WHERE \"s\".code = \"t\".code) ");
        expected.Append("\"x\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Cte_ColumnFromSourceColumn_CorrectSql()
    {
        Cte cte = new("cte");

        SqlStatement sql =
            With(cte.As(Select(_a.Code).From(_a)))
            .Select(cte.Column(_a.Code))
                .From(cte)
            .Build();

        StringBuilder expected = new();
        expected.Append("WITH \"cte\" AS ");
        expected.Append("(SELECT \"a\".code FROM test_table \"a\") ");
        expected.Append("SELECT \"cte\".code ");
        expected.Append("FROM \"cte\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Cte_ColumnFromAlias_CorrectSql()
    {
        Cte cte = new("cte");
        ExpressionAlias code = _a.Code.As("c");

        SqlStatement sql =
            With(cte.As(Select(code).From(_a)))
            .Select(cte.Column(code))
                .From(cte)
            .Build();

        StringBuilder expected = new();
        expected.Append("WITH \"cte\" AS ");
        expected.Append("(SELECT \"a\".code \"c\" FROM test_table \"a\") ");
        expected.Append("SELECT \"cte\".c ");
        expected.Append("FROM \"cte\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
