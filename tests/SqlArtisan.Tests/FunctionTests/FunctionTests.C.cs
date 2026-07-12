using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public partial class FunctionTests
{
    [Fact]
    public void Ceil_NumericValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Ceil(_t.Code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CEIL(\"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    // Ceil() emits CEIL verbatim on every DBMS: the SQL you write is the SQL
    // that runs. It is not rewritten to CEILING on SQL Server.
    [Fact]
    public void Ceil_SqlServer_StillEmitsCeil()
    {
        SqlStatement sql =
            Select(Ceil(_t.Code))
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CEIL(\"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Ceiling_NumericValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Ceiling(_t.Code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CEILING(\"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    // Ceiling() emits CEILING verbatim on every DBMS, including Oracle, where it
    // is spelled CEIL: the choice of spelling is the caller's, not the dialect's.
    [Fact]
    public void Ceiling_Oracle_StillEmitsCeiling()
    {
        SqlStatement sql =
            Select(Ceiling(_t.Code))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CEILING(\"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Coalesce_MultipleValues_CorrectSql()
    {
        SqlStatement sql =
            Select(Coalesce(_t.Code, _t.Name, "other"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("COALESCE(\"t\".code, \"t\".name, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Concat_TwoValues_CorrectSql()
    {
        SqlStatement sql =
            Select(Concat(_t.Name, "a"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CONCAT(\"t\".name, :0)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Concat_MultipleValues_CorrectSql()
    {
        SqlStatement sql =
            Select(Concat(_t.Name, "a", "b"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CONCAT(\"t\".name, :0, :1)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Contains_SqlServer_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where(Contains(_t.Name, "database"))
            .Build(Dbms.SqlServer);

        StringBuilder expected = new();
        expected.Append("SELECT \"t\".code ");
        expected.Append("FROM test_table \"t\" ");
        expected.Append("WHERE CONTAINS(\"t\".name, @0)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("database", sql.Parameters.Get<string>("@0"));
    }

    [Fact]
    public void ContainsScore_Oracle_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Code)
            .From(_t)
            .Where(ContainsScore(_t.Name, "database") > 0)
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT \"t\".code ");
        expected.Append("FROM test_table \"t\" ");
        expected.Append("WHERE CONTAINS(\"t\".name, :0) > :1");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("database", sql.Parameters.Get<string>(":0"));
        Assert.Equal(0, sql.Parameters.Get<int>(":1"));
    }

    [Fact]
    public void ContainsScore_Oracle_WithLabel_CorrectSql()
    {
        SqlStatement sql =
            Select(ContainsScore(_t.Name, "database", 1))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CONTAINS(\"t\".name, :0, 1)");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("database", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void Count_NoArgument_CorrectSql()
    {
        SqlStatement sql =
            Select(Count())
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("COUNT(*)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Count_Asterisk_CorrectSql()
    {
        SqlStatement sql =
            Select(Count(Asterisk))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("COUNT(*)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Count_ColumnValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Count(_t.Code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("COUNT(\"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Count_DistinctColumnValue_CorrectSql()
    {
        SqlStatement sql =
            Select(Count(Distinct, _t.Code))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("COUNT(DISTINCT \"t\".code)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void CurrentDate_NoParameters_CorrectSql()
    {
        SqlStatement sql = Select(CurrentDate).Build();
        Assert.Equal("SELECT CURRENT_DATE", sql.Text);
    }

    [Fact]
    public void CurrentTime_NoParameters_CorrectSql()
    {
        SqlStatement sql = Select(CurrentTime).Build();
        Assert.Equal("SELECT CURRENT_TIME", sql.Text);
    }

    [Fact]
    public void CurrentTimestamp_NoParameters_CorrectSql()
    {
        SqlStatement sql = Select(CurrentTimestamp).Build();
        Assert.Equal("SELECT CURRENT_TIMESTAMP", sql.Text);
    }

    [Fact]
    public void Currval_SequenceName_CorrectSql()
    {
        SqlStatement sql =
            Select(Currval("seq_test"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("CURRVAL('seq_test')");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
