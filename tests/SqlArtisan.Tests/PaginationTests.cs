using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class PaginationTests
{
    private readonly TestTable _t = new();

    // ── LIMIT family (PostgreSQL / MySQL / SQLite) ────────────────────

    [Fact]
    public void Limit_CorrectSql()
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
    public void Offset_CorrectSql()
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
    public void LimitOffset_CorrectSql()
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
    public void FetchFirst_CorrectSql()
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
    public void OffsetRows_CorrectSql()
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
    public void OffsetRowsFetchNext_CorrectSql()
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

    [Fact]
    public void FetchFirst_UsesDialectParameterMarker()
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
