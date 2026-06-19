using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class UpdateTests
{
    private readonly TestTable _t = new();

    [Fact]
    public void Update_SetLiterals_CorrectSql()
    {
        SqlStatement sql =
            Update(_t)
            .Set(
                _t.Code == 1,
                _t.Name == "a",
                _t.CreatedAt == Sysdate)
            .Build();

        StringBuilder expected = new();
        expected.Append("UPDATE ");
        expected.Append("test_table ");
        expected.Append("SET ");
        expected.Append("code = :0, ");
        expected.Append("name = :1, ");
        expected.Append("created_at = SYSDATE");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Update_SetMultipleVariables_CorrectSql()
    {
        TestTableDto dto = new(1, "Test1", new DateTime(2001, 2, 3));

        SqlStatement sql =
            Update(_t)
            .Set(
                _t.Code == dto.Code,
                _t.Name == dto.Name,
                _t.CreatedAt == dto.CreatedAt)
            .Build();

        StringBuilder expected = new();
        expected.Append("UPDATE ");
        expected.Append("test_table ");
        expected.Append("SET ");
        expected.Append("code = :0, ");
        expected.Append("name = :1, ");
        expected.Append("created_at = :2");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
        Assert.Equal("Test1", sql.Parameters.Get<string>(":1"));
        Assert.Equal(new DateTime(2001, 2, 3), sql.Parameters.Get<DateTime>(":2"));
    }

    [Fact]
    public void Update_WithAlias_CorrectSql()
    {
        // An aliased UPDATE declares the alias with AS on the target; the SET
        // left side stays unqualified (PostgreSQL rejects `SET x.col = ...`),
        // while the WHERE predicate qualifies through the alias.
        TestTable t = new("t");

        SqlStatement sql =
            Update(t)
            .Set(t.Name == "a")
            .Where(t.Code == 1)
            .Build(Dbms.PostgreSql);

        StringBuilder expected = new();
        expected.Append("UPDATE ");
        expected.Append("test_table AS \"t\" ");
        expected.Append("SET ");
        expected.Append("name = :0 ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code = :1");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Update_Oracle_WithAlias_CorrectSql()
    {
        // Oracle rejects AS on a table alias (ORA-00933): the alias follows the
        // table name with only a space.
        TestTable t = new("t");

        SqlStatement sql =
            Update(t)
            .Set(t.Name == "a")
            .Where(t.Code == 1)
            .Build(Dbms.Oracle);

        StringBuilder expected = new();
        expected.Append("UPDATE ");
        expected.Append("test_table \"t\" ");
        expected.Append("SET ");
        expected.Append("name = :0 ");
        expected.Append("WHERE ");
        expected.Append("\"t\".code = :1");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Update_SetWithInequality_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            Update(_t)
            .Set(
                _t.Code != 1)
            .Build();
        });
    }
}
