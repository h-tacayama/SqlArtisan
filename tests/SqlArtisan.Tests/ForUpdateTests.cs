using System.Text;
using SqlArtisan.Internal;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class ForUpdateTests
{
    [Fact]
    public void ForUpdate_NoOptions_CorrectSql()
    {
        TestTable t = new();
        SqlStatement sql =
            Select(t.Name)
            .From(t)
            .ForUpdate()
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("name ");
        expected.Append("FROM ");
        expected.Append("test_table ");
        expected.Append("FOR UPDATE");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ForUpdate_NoWait_CorrectSql()
    {
        TestTable t = new();
        SqlStatement sql =
            Select(t.Name)
            .From(t)
            .ForUpdate(Nowait)
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("name ");
        expected.Append("FROM ");
        expected.Append("test_table ");
        expected.Append("FOR UPDATE NOWAIT");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ForUpdate_SkipLocked_CorrectSql()
    {
        TestTable t = new();
        SqlStatement sql =
            Select(t.Name)
            .From(t)
            .ForUpdate(SkipLocked)
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("name ");
        expected.Append("FROM ");
        expected.Append("test_table ");
        expected.Append("FOR UPDATE SKIP LOCKED");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ForUpdate_WaitSeconds_CorrectSql()
    {
        TestTable t = new();
        SqlStatement sql =
            Select(t.Name)
            .From(t)
            .ForUpdate(Wait(5))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("name ");
        expected.Append("FROM ");
        expected.Append("test_table ");
        expected.Append("FOR UPDATE WAIT 5");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ForUpdate_WaitZeroSeconds_CorrectSql()
    {
        TestTable t = new();
        SqlStatement sql =
            Select(t.Name)
            .From(t)
            .ForUpdate(Wait(0))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("name ");
        expected.Append("FROM ");
        expected.Append("test_table ");
        expected.Append("FOR UPDATE WAIT 0");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Wait_NegativeSeconds_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Wait(-1));

        Assert.Equal("WAIT requires a non-negative number of seconds.", exception.Message);
    }

    [Fact]
    public void ForUpdate_Of_CorrectSql()
    {
        TestTable t = new();
        SqlStatement sql =
            Select(t.Name)
            .From(t)
            .ForUpdate(Of(t.Code))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("name ");
        expected.Append("FROM ");
        expected.Append("test_table ");
        expected.Append("FOR UPDATE OF code");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ForUpdate_OfAndNoWait_CorrectSql()
    {
        TestTable t = new();
        SqlStatement sql =
            Select(t.Name)
            .From(t)
            .ForUpdate(Of(t.Code), Nowait)
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("name ");
        expected.Append("FROM ");
        expected.Append("test_table ");
        expected.Append("FOR UPDATE OF code NOWAIT");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ForUpdate_OfAndSkipLocked_CorrectSql()
    {
        TestTable t = new();
        SqlStatement sql =
            Select(t.Name)
            .From(t)
            .ForUpdate(Of(t.Code), SkipLocked)
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("name ");
        expected.Append("FROM ");
        expected.Append("test_table ");
        expected.Append("FOR UPDATE OF code SKIP LOCKED");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ForUpdate_OfAndWaitSeconds_CorrectSql()
    {
        TestTable t = new();
        SqlStatement sql =
            Select(t.Name)
            .From(t)
            .ForUpdate(Of(t.Code), Wait(5))
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("name ");
        expected.Append("FROM ");
        expected.Append("test_table ");
        expected.Append("FOR UPDATE OF code WAIT 5");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ForUpdate_AfterLimit_CorrectSql()
    {
        TestTable t = new();
        SqlStatement sql =
            Select(t.Code)
            .From(t)
            .OrderBy(t.Code)
            .Limit(1)
            .ForUpdate()
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT code ");
        expected.Append("FROM test_table ");
        expected.Append("ORDER BY code ");
        expected.Append("LIMIT :0 ");
        expected.Append("FOR UPDATE");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(1, sql.Parameters.Count);
        Assert.Equal(1, sql.Parameters.Get<object>(":0"));
    }

    [Fact]
    public void ForUpdate_SkipLockedAfterLimit_CorrectSql()
    {
        // The queue-worker claim: take one unlocked row and lock it (#520).
        TestTable t = new();
        SqlStatement sql =
            Select(t.Code)
            .From(t)
            .OrderBy(t.Code)
            .Limit(1)
            .ForUpdate(SkipLocked)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT code ");
        expected.Append("FROM test_table ");
        expected.Append("ORDER BY code ");
        expected.Append("LIMIT :0 ");
        expected.Append("FOR UPDATE SKIP LOCKED");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(1, sql.Parameters.Count);
        Assert.Equal(1, sql.Parameters.Get<object>(":0"));
    }

    [Fact]
    public void ForUpdate_AfterLimitOffset_CorrectSql()
    {
        TestTable t = new();
        SqlStatement sql =
            Select(t.Code)
            .From(t)
            .OrderBy(t.Code)
            .Limit(10)
            .Offset(20)
            .ForUpdate()
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT code ");
        expected.Append("FROM test_table ");
        expected.Append("ORDER BY code ");
        expected.Append("LIMIT :0 OFFSET :1 ");
        expected.Append("FOR UPDATE");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(2, sql.Parameters.Count);
        Assert.Equal(10, sql.Parameters.Get<object>(":0"));
        Assert.Equal(20, sql.Parameters.Get<object>(":1"));
    }

    [Fact]
    public void ForUpdate_AfterFetchFirst_CorrectSql()
    {
        TestTable t = new();
        SqlStatement sql =
            Select(t.Code)
            .From(t)
            .OrderBy(t.Code)
            .FetchFirst(1)
            .ForUpdate()
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT code ");
        expected.Append("FROM test_table ");
        expected.Append("ORDER BY code ");
        expected.Append("FETCH FIRST :0 ROWS ONLY ");
        expected.Append("FOR UPDATE");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(1, sql.Parameters.Count);
        Assert.Equal(1, sql.Parameters.Get<object>(":0"));
    }

    [Fact]
    public void ForUpdate_AfterOffsetRowsFetchNext_CorrectSql()
    {
        TestTable t = new();
        SqlStatement sql =
            Select(t.Code)
            .From(t)
            .OrderBy(t.Code)
            .OffsetRows(20)
            .FetchNext(10)
            .ForUpdate()
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT code ");
        expected.Append("FROM test_table ");
        expected.Append("ORDER BY code ");
        expected.Append("OFFSET :0 ROWS FETCH NEXT :1 ROWS ONLY ");
        expected.Append("FOR UPDATE");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(2, sql.Parameters.Count);
        Assert.Equal(20, sql.Parameters.Get<object>(":0"));
        Assert.Equal(10, sql.Parameters.Get<object>(":1"));
    }

    [Fact]
    public void ForUpdate_NullOfClause_ThrowsArgumentNullException()
    {
        // A null OF list would silently widen the lock to every table.
        TestTable t = new();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            Select(t.Name).From(t).ForUpdate((OfClause)null!));

        Assert.Equal("ofClause", ex.ParamName);
    }

    // #521: PostgreSQL's and MySQL's OF names the relation — the alias when the
    // table has one, since both engines reject the table name then.
    [Fact]
    public void ForUpdate_OfTables_PostgreSql_CorrectSql()
    {
        TestTable t = new("t");
        TestTable s = new("s");
        SqlStatement sql =
            Select(t.Name)
            .From(t)
            .InnerJoin(s).On(t.Code == s.Code)
            .ForUpdate(Of(t, s), SkipLocked)
            .Build(Dbms.PostgreSql);

        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" "
                + "INNER JOIN test_table \"s\" ON \"t\".code = \"s\".code "
                + "FOR UPDATE OF \"t\", \"s\" SKIP LOCKED",
            sql.Text);
    }

    [Fact]
    public void ForUpdate_OfTable_MySql_QuotesTheAliasInBackticks()
    {
        TestTable t = new("t");
        SqlStatement sql = Select(t.Name).From(t).ForUpdate(Of(t), Nowait).Build(Dbms.MySql);

        Assert.Equal("SELECT `t`.name FROM test_table `t` FOR UPDATE OF `t` NOWAIT", sql.Text);
    }

    [Fact]
    public void ForUpdate_OfUnaliasedTable_RendersTheTableName()
    {
        TestTable t = new();
        SqlStatement sql = Select(t.Name).From(t).ForUpdate(Of(t)).Build(Dbms.PostgreSql);

        Assert.Equal("SELECT name FROM test_table FOR UPDATE OF test_table", sql.Text);
    }

    [Fact]
    public void Of_NullTable_ThrowsArgumentNullException()
    {
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => Of((DbTableBase)null!));

        Assert.Equal("table", exception.ParamName);
    }

    [Fact]
    public void Of_NullInMoreTables_ThrowsArgumentNullException()
    {
        TestTable t = new("t");

        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => Of(t, (DbTableBase)null!));

        Assert.Equal(
            "A FOR UPDATE OF list must not contain a null table. (Parameter 'moreTables')",
            exception.Message);
    }
}
