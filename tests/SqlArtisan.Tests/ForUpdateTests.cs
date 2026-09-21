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
    public void ForUpdate_NullOfClause_ThrowsArgumentNullException()
    {
        // A null OF list would silently widen the lock to every table.
        TestTable t = new();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            Select(t.Name).From(t).ForUpdate((OfClause)null!));

        Assert.Equal("ofClause", ex.ParamName);
    }
}
