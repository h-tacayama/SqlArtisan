using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class ForUpdateTest
{
    private readonly TestTable _t = new("t");

    [Fact]
    public void ForUpdate_NoOptions_CorrectSql()
    {
        TestTable t = new();
        SqlStatement sql =
            Select(t.Name)
            .From(t)
            .ForUpdate()
            .Build();

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
            .Build();

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
            .Build();

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
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("name ");
        expected.Append("FROM ");
        expected.Append("test_table ");
        expected.Append("FOR UPDATE WAIT 5");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void ForUpdate_Of_CorrectSql()
    {
        TestTable t = new();
        SqlStatement sql =
            Select(t.Name)
            .From(t)
            .ForUpdate(Of(t.Code))
            .Build();

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
            .Build();

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
            .Build();

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
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("name ");
        expected.Append("FROM ");
        expected.Append("test_table ");
        expected.Append("FOR UPDATE OF code WAIT 5");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
