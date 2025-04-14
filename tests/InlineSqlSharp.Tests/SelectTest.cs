using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class SelectTest
{
    private readonly test_table _t = new("t");

    [Fact]
    public void SELECT_ColumnAliases_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                _t.code.AS("code"),
                _t.name.AS("name"),
                _t.created_at.AS("登録日"))
            .FROM(_t)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code AS \"code\", ");
        expected.Append("\"t\".name AS \"name\", ");
        expected.Append("\"t\".created_at AS \"登録日\" ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void SELECT_DistinctFromClause_CorrectSql()
    {
        SqlStatement sql =
            SELECT(DISTINCT, _t.code)
            .FROM(_t)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("DISTINCT ");
        expected.Append("\"t\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void SELECT_FromClauseWithMultipleColumns_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                _t.code,
                _t.name,
                _t.created_at)
            .FROM(_t)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".name, ");
        expected.Append("\"t\".created_at ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void SELECT_FromDualClause_CorrectSql()
    {
        SqlStatement sql =
            SELECT(SYSDATE)
            .FROM(DUAL)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("SYSDATE ");
        expected.Append("FROM ");
        expected.Append("DUAL");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void SELECT_Literals_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                L('a'),
                L("O''Reilly"),
                L((sbyte)1),
                L((byte)2),
                L((short)3),
                L((ushort)4),
                L((int)5),
                L((uint)6),
                L((long)7),
                L((ulong)8),
                L((float)9.9),
                L((double)10.10),
                L((decimal)11.11))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("'a', ");
        expected.Append("'O''Reilly', ");
        expected.Append("1, ");
        expected.Append("2, ");
        expected.Append("3, ");
        expected.Append("4, ");
        expected.Append("5, ");
        expected.Append("6, ");
        expected.Append("7, ");
        expected.Append("8, ");
        expected.Append("9.9, ");
        expected.Append("10.1, ");
        expected.Append("11.11");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void SELECT_Null_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                NULL,
                NULL.AS("e"))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT NULL, NULL AS \"e\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void SELECT_Parameters_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                P('a'),
                P("O''Reilly"),
                P(new DateTime(2001, 2, 3)),
                P((sbyte)1),
                P((byte)2),
                P((short)3),
                P((ushort)4),
                P((int)5),
                P((uint)6),
                P((long)7),
                P((ulong)8),
                P((float)9.9),
                P((double)10.10),
                P((decimal)11.11))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append(":0, ");
        expected.Append(":1, ");
        expected.Append(":2, ");
        expected.Append(":3, ");
        expected.Append(":4, ");
        expected.Append(":5, ");
        expected.Append(":6, ");
        expected.Append(":7, ");
        expected.Append(":8, ");
        expected.Append(":9, ");
        expected.Append(":10, ");
        expected.Append(":11, ");
        expected.Append(":12, ");
        expected.Append(":13");

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal("a", sql.Parameters.Get<string>(":0"));
        Assert.Equal("O''Reilly", sql.Parameters.Get<string>(":1"));
        Assert.Equal(new DateTime(2001, 2, 3), sql.Parameters.Get<DateTime>(":2"));
        Assert.Equal((sbyte)1, sql.Parameters.Get<sbyte>(":3"));
        Assert.Equal((byte)2, sql.Parameters.Get<byte>(":4"));
        Assert.Equal((short)3, sql.Parameters.Get<short>(":5"));
        Assert.Equal((ushort)4, sql.Parameters.Get<ushort>(":6"));
        Assert.Equal((int)5, sql.Parameters.Get<int>(":7"));
        Assert.Equal((uint)6, sql.Parameters.Get<uint>(":8"));
        Assert.Equal((long)7, sql.Parameters.Get<long>(":9"));
        Assert.Equal((ulong)8, sql.Parameters.Get<ulong>(":10"));
        Assert.Equal((float)9.9, sql.Parameters.Get<float>(":11"));
        Assert.Equal((double)10.1, sql.Parameters.Get<double>(":12"));
        Assert.Equal((decimal)11.11, sql.Parameters.Get<decimal>(":13"));
    }

    [Fact]
    public void SELECT_SequenceValues_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                SEQUENCE("seq").CURRVAL,
                SEQUENCE("seq").NEXTVAL)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("seq.CURRVAL, ");
        expected.Append("seq.NEXTVAL");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void SELECT_TableAliasWithDoubleQuotes_CorrectSql()
    {
        test_table _t = new("t s");

        SqlStatement sql =
            SELECT(_t.code)
            .FROM(_t)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t s\".code ");
        expected.Append("FROM ");
        expected.Append("test_table \"t s\"");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void SELECT_WithHints_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                HINTS("/*+ ANY HINT */"),
                _t.code)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("/*+ ANY HINT */ ");
        expected.Append("\"t\".code");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void SELECT_WithHintsAndDistinct_CorrectSql()
    {
        SqlStatement sql =
            SELECT(
                HINTS("/*+ ANY HINT */"),
                DISTINCT,
                _t.code)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("/*+ ANY HINT */ ");
        expected.Append("DISTINCT ");
        expected.Append("\"t\".code");

        Assert.Equal(expected.ToString(), sql.Text);
    }
}
