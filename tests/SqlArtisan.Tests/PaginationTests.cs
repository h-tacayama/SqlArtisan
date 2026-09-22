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

    // ── OFFSET/FETCH family (Oracle / PostgreSql / SqlServer) ──────────

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
    public void OffsetRowsFetchNext_SqlServer_UsesDialectParameterMarker()
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

    // MySQL and SQLite take OFFSET only after LIMIT (live-verified, release audit
    // pass 8); the analyzer's Offset key is a union, so Build(Dbms) carries the guard.
    [Theory]
    [InlineData(Dbms.MySql)]
    [InlineData(Dbms.Sqlite)]
    public void Offset_WithoutLimit_ThrowsArgumentException(Dbms dbms)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Select(_t.Code).From(_t).OrderBy(_t.Code).Offset(5).Build(dbms));

        Assert.Equal(
            "MySQL and SQLite accept OFFSET only after LIMIT; add Limit(...) before Offset(...).",
            ex.Message);
    }

    [Fact]
    public void Offset_WithoutLimit_PostgreSql_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code).From(_t).OrderBy(_t.Code).Offset(5).Build(Dbms.PostgreSql);

        Assert.Equal("SELECT code FROM test_table ORDER BY code OFFSET :0", sql.Text);
        Assert.Equal(5, sql.Parameters.Get<int>(":0"));
    }

    // ── Negative row counts stay permissive (ADR 0012 non-goal, #523) ─

    [Fact]
    public void Top_NegativeCount_BindsTheCountRatherThanPrintingIt()
    {
        // The count is a bind parameter, never statement text, so ADR 0012's
        // literal-embedded condition excludes it — the engine is the arbiter.
        SqlStatement sql = Select(Top(-1), _t.Code).From(_t).Build(Dbms.SqlServer);

        Assert.Equal("SELECT TOP (@0) code FROM test_table", sql.Text);
        Assert.Equal(-1, sql.Parameters.Get<int>("@0"));
    }

    [Fact]
    public void FetchFirst_NegativeCount_BindsTheCountRatherThanPrintingIt()
    {
        // Oracle XE 21.3.0 takes a negative FETCH count outright, so no
        // universally-invalid domain exists to guard (live-verified, #523).
        SqlStatement sql =
            Select(_t.Code).From(_t).OrderBy(_t.Code).FetchFirst(-1).Build(Dbms.Oracle);

        Assert.Equal(
            "SELECT code FROM test_table ORDER BY code FETCH FIRST :0 ROWS ONLY", sql.Text);
        Assert.Equal(-1, sql.Parameters.Get<int>(":0"));
    }

    [Theory]
    [InlineData(false, "SELECT code FROM test_table ORDER BY code OFFSET :0")]
    [InlineData(true, "SELECT code FROM test_table ORDER BY code OFFSET :0 ROWS")]
    public void Offset_NegativeStart_BindsTheStartRatherThanPrintingIt(
        bool rowsForm, string expected)
    {
        // SQLA0104 leaves the OFFSET family out on the strength of this: a bound
        // start is one Build(Dbms) cannot see, so no guard can reach it (#532).
        SqlStatement sql = rowsForm
            ? Select(_t.Code).From(_t).OrderBy(_t.Code).OffsetRows(-1).Build()
            : Select(_t.Code).From(_t).OrderBy(_t.Code).Offset(-1).Build();

        Assert.Equal(expected, sql.Text);
        Assert.Equal(-1, sql.Parameters.Get<int>(":0"));
    }
}
