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
	public void SELECT_LiteralValues_CorrectSql()
	{
		SqlStatement sql =
			SELECT(
				L('a'),
				L("O''Reilly"),
				L((int)1),
				L((long)2),
				L((float)3.3),
				L((double)4.4),
				L((decimal)5.5))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("'a', ");
		expected.Append("'O''Reilly', ");
		expected.Append("1, ");
		expected.Append("2, ");
		expected.Append("3.3, ");
		expected.Append("4.4, ");
		expected.Append("5.5");

		Assert.Equal(expected.ToString(), sql.Text);
	}

	[Fact]
	public void SELECT_ParameterValues_CorrectSql()
	{
		SqlStatement sql =
			SELECT(
				P('a'),
				P("O''Reilly"),
				P(new DateTime(2001, 2, 3)),
				P((int)1),
				P((long)2),
				P((float)3.3),
				P((double)4.4),
				P((decimal)5.5))
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
		expected.Append(":7");

		Assert.Equal(expected.ToString(), sql.Text);
		Assert.Equal("a", sql.Parameters[0].Value);
		Assert.Equal("O''Reilly", sql.Parameters[1].Value);
		Assert.Equal(new DateTime(2001, 2, 3), sql.Parameters[2].Value);
		Assert.Equal((int)1, sql.Parameters[3].Value);
		Assert.Equal((long)2, sql.Parameters[4].Value);
		Assert.Equal((float)3.3, sql.Parameters[5].Value);
		Assert.Equal((double)4.4, sql.Parameters[6].Value);
		Assert.Equal((decimal)5.5, sql.Parameters[7].Value);
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
