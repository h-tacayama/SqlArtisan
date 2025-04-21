using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class UpdateTest
{
    private readonly test_table _t = new("t");

    [Fact]
    public void UPDATE_SetLiterals_CorrectSql()
    {
        SqlStatement sql =
            UPDATE(_t)
            .SET(
                _t.code == 1,
                _t.name == "a",
                _t.created_at == SYSDATE)
            .Build();

        StringBuilder expected = new();
        expected.Append("UPDATE ");
        expected.Append("test_table \"t\" ");
        expected.Append("SET ");
        expected.Append("\"t\".code = :0, ");
        expected.Append("\"t\".name = :1, ");
        expected.Append("\"t\".created_at = SYSDATE");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void UPDATE_SetMultipleVariables_CorrectSql()
    {
        TestTableDto dto = new(1, "Test1", new DateTime(2001, 2, 3));

        SqlStatement sql =
            UPDATE(_t)
            .SET(
                _t.code == dto.Code,
                _t.name == dto.Name,
                _t.created_at == dto.CreatedAt)
            .Build();

        StringBuilder expected = new();
        expected.Append("UPDATE ");
        expected.Append("test_table \"t\" ");
        expected.Append("SET ");
        expected.Append("\"t\".code = :0, ");
        expected.Append("\"t\".name = :1, ");
        expected.Append("\"t\".created_at = :2");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(1, sql.Parameters.Get<int>(":0"));
        Assert.Equal("Test1", sql.Parameters.Get<string>(":1"));
        Assert.Equal(new DateTime(2001, 2, 3), sql.Parameters.Get<DateTime>(":2"));
    }

    [Fact]
    public void UPDATE_SetWithInequality_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            UPDATE(_t)
            .SET(
                _t.code != 1)
            .Build();
        });
    }
}
