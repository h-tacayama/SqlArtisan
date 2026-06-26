using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class DeleteTests
{
    private readonly TestTable _t = new("t");

    [Fact]
    public void DeleteFrom_SimpleTable_CorrectSql()
    {
        SqlStatement sql =
            DeleteFrom(_t)
            .Build();

        StringBuilder expected = new();
        expected.Append("DELETE FROM ");
        expected.Append("test_table AS \"t\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DeleteFrom_WithWhereClause_CorrectSql()
    {
        SqlStatement sql =
            DeleteFrom(_t)
            .Where(_t.Code == 1)
            .Build();

        StringBuilder expected = new();
        expected.Append("DELETE FROM ");
        expected.Append("test_table AS \"t\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code = :0");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void DeleteFrom_Oracle_WithWhereClause_CorrectSql()
    {
        // Oracle rejects AS on a table alias (ORA-00933), so the alias follows
        // the table name with only a space.
        SqlStatement sql =
            DeleteFrom(_t)
            .Where(_t.Code == 1)
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("DELETE FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code = :0");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
