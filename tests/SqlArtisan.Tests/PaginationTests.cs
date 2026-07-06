using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class PaginationTests
{
    private readonly TestTable _t = new();

    // ── LIMIT family (PostgreSQL / MySQL / SQLite) ────────────────────

    [Fact]
    public void Limit_WithOrderBy_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .OrderBy(_t.Code)
            .Limit(10)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT code ");
        expected.Append("FROM test_table ");
        expected.Append("ORDER BY code ");
        expected.Append("LIMIT :0");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Offset_WithOrderBy_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .OrderBy(_t.Code)
            .Offset(20)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT code ");
        expected.Append("FROM test_table ");
        expected.Append("ORDER BY code ");
        expected.Append("OFFSET :0");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void LimitOffset_WithOrderBy_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .OrderBy(_t.Code)
            .Limit(10)
            .Offset(20)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT code ");
        expected.Append("FROM test_table ");
        expected.Append("ORDER BY code ");
        expected.Append("LIMIT :0 OFFSET :1");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Limit_WithoutOrderBy_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Limit(10)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT code ");
        expected.Append("FROM test_table ");
        expected.Append("LIMIT :0");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Limit_AfterWhere_ParametersInOrder()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where(_t.Code > 0)
            .OrderBy(_t.Code)
            .Limit(10)
            .Offset(20)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT code ");
        expected.Append("FROM test_table ");
        expected.Append("WHERE code > :0 ");
        expected.Append("ORDER BY code ");
        expected.Append("LIMIT :1 OFFSET :2");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    // ── OFFSET/FETCH family (Oracle 12c+ / SQL Server 2012+) ──────────

    [Fact]
    public void FetchFirst_WithOrderBy_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .OrderBy(_t.Code)
            .FetchFirst(10)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT code ");
        expected.Append("FROM test_table ");
        expected.Append("ORDER BY code ");
        expected.Append("FETCH FIRST :0 ROWS ONLY");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void OffsetRows_WithOrderBy_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .OrderBy(_t.Code)
            .OffsetRows(20)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT code ");
        expected.Append("FROM test_table ");
        expected.Append("ORDER BY code ");
        expected.Append("OFFSET :0 ROWS");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void OffsetRowsFetchNext_WithOrderBy_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .OrderBy(_t.Code)
            .OffsetRows(20)
            .FetchNext(10)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT code ");
        expected.Append("FROM test_table ");
        expected.Append("ORDER BY code ");
        expected.Append("OFFSET :0 ROWS FETCH NEXT :1 ROWS ONLY");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    // ── Row-limited queries as subqueries (#240) ──────────────────────

    [Fact]
    public void Limit_SubqueryInCrossJoinLateral_CorrectSql()
    {
        TestTable t = new("t");
        TestTable s = new("s");
        DerivedTable x = new("x");

        SqlStatement sql =
            Select(t.Name, x.Column("code"))
            .From(t)
            .CrossJoinLateral(
                Select(s.Code.As(x.Column("code")))
                    .From(s)
                    .Where(s.Code == t.Code)
                    .OrderBy(s.Code)
                    .Limit(3),
                x)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name, \"x\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("CROSS JOIN LATERAL ");
        expected.Append("(");
        expected.Append("SELECT \"s\".code code FROM test_table \"s\" ");
        expected.Append("WHERE \"s\".code = \"t\".code ");
        expected.Append("ORDER BY \"s\".code LIMIT :0");
        expected.Append(") ");
        expected.Append("\"x\"");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(1, sql.Parameters.Count);
        Assert.Equal(3, sql.Parameters.Get<object>(":0"));
    }

    [Fact]
    public void FetchNext_SqlServer_SubqueryInCrossApply_CorrectSql()
    {
        TestTable t = new("t");
        TestTable s = new("s");
        DerivedTable x = new("x");

        SqlStatement sql =
            Select(t.Name, x.Column("code"))
            .From(t)
            .CrossApply(
                Select(s.Code.As(x.Column("code")))
                    .From(s)
                    .Where(s.Code == t.Code)
                    .OrderBy(s.Code)
                    .OffsetRows(0)
                    .FetchNext(3),
                x)
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name, \"x\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("CROSS APPLY ");
        expected.Append("(");
        expected.Append("SELECT \"s\".code code FROM test_table \"s\" ");
        expected.Append("WHERE \"s\".code = \"t\".code ");
        expected.Append("ORDER BY \"s\".code OFFSET @0 ROWS FETCH NEXT @1 ROWS ONLY");
        expected.Append(") ");
        expected.Append("\"x\"");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(2, sql.Parameters.Count);
        Assert.Equal(0, sql.Parameters.Get<object>("@0"));
        Assert.Equal(3, sql.Parameters.Get<object>("@1"));
    }

    [Fact]
    public void Limit_AliasedScalarSubquery_CorrectSql()
    {
        TestTable t = new("t");
        TestTable s = new("s");

        SqlStatement sql =
            Select(
                t.Name,
                Select(s.Code).From(s).OrderBy(s.Code).Limit(1).As("top_code"))
            .From(t)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name, ");
        expected.Append("(SELECT \"s\".code FROM test_table \"s\" ");
        expected.Append("ORDER BY \"s\".code LIMIT :0) \"top_code\" ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\"");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(1, sql.Parameters.Count);
        Assert.Equal(1, sql.Parameters.Get<object>(":0"));
    }

    [Fact]
    public void Limit_SubqueryInCteAs_CorrectSql()
    {
        TestTable s = new("s");
        Cte c = new("c");

        SqlStatement sql =
            With(
                c.As(
                    Select(s.Code.As(c.Column("code")))
                    .From(s)
                    .OrderBy(s.Code)
                    .Limit(3)))
            .Select(c.Column("code"))
            .From(c)
            .Build();

        StringBuilder expected = new();
        expected.Append("WITH \"c\" AS ");
        expected.Append("(");
        expected.Append("SELECT \"s\".code code FROM test_table \"s\" ");
        expected.Append("ORDER BY \"s\".code LIMIT :0");
        expected.Append(") ");
        expected.Append("SELECT \"c\".code ");
        expected.Append("FROM \"c\"");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(1, sql.Parameters.Count);
        Assert.Equal(3, sql.Parameters.Get<object>(":0"));
    }

    [Fact]
    public void OffsetRowsFetchNext_OnSqlServer_UsesDialectParameterMarker()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .OrderBy(_t.Code)
            .OffsetRows(20)
            .FetchNext(10)
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("SELECT code ");
        expected.Append("FROM test_table ");
        expected.Append("ORDER BY code ");
        expected.Append("OFFSET @0 ROWS FETCH NEXT @1 ROWS ONLY");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
