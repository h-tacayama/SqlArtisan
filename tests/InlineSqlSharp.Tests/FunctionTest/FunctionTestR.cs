using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public partial class FunctionTest
{
	[Fact]
	public void REGEXP_COUNT_Pattern_CorrectSql()
	{
		SqlCommand sql =
			SELECT(REGEXP_COUNT(_t.name, L("[abc]")))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("REGEXP_COUNT(t.name, '[abc]')");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void REGEXP_COUNT_PatternPosition_CorrectSql()
	{
		SqlCommand sql =
			SELECT(REGEXP_COUNT(_t.name, L("[abc]"), L(2)))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("REGEXP_COUNT(t.name, '[abc]', 2)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void REGEXP_COUNT_PatternPositionOptions_CorrectSql()
	{
		SqlCommand sql =
			SELECT(REGEXP_COUNT(_t.name, L("[abc]"), L(2), RegexpOptions.CaseInsensitive))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("REGEXP_COUNT(t.name, '[abc]', 2, 'i')");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void REGEXP_REPLACE_PatternReplacement_CorrectSql()
	{
		SqlCommand sql =
			SELECT(REGEXP_REPLACE(_t.name, L("[abc]"), L("x")))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("REGEXP_REPLACE(t.name, '[abc]', 'x')");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void REGEXP_REPLACE_PatternReplacementPosition_CorrectSql()
	{
		SqlCommand sql =
			SELECT(REGEXP_REPLACE(_t.name, L("[abc]"), L("x"), L(2)))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("REGEXP_REPLACE(t.name, '[abc]', 'x', 2)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void REGEXP_REPLACE_PatternReplacementPositionOccurrence_CorrectSql()
	{
		SqlCommand sql =
			SELECT(REGEXP_REPLACE(_t.name, L("[abc]"), L("x"), L(2), L(3)))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("REGEXP_REPLACE(t.name, '[abc]', 'x', 2, 3)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void REGEXP_REPLACE_PatternReplacementPositionOccurrenceOptions_CorrectSql()
	{
		SqlCommand sql =
			SELECT(REGEXP_REPLACE(_t.name, L("[abc]"), L("x"), L(2), L(3), RegexpOptions.CaseInsensitive))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("REGEXP_REPLACE(t.name, '[abc]', 'x', 2, 3, 'i')");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void REGEXP_SUBSTR_Pattern_CorrectSql()
	{
		SqlCommand sql =
			SELECT(REGEXP_SUBSTR(_t.name, L("[abc]")))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("REGEXP_SUBSTR(t.name, '[abc]')");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void REGEXP_SUBSTR_PatternPosition_CorrectSql()
	{
		SqlCommand sql =
			SELECT(REGEXP_SUBSTR(_t.name, L("[abc]"), L(2)))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("REGEXP_SUBSTR(t.name, '[abc]', 2)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void REGEXP_SUBSTR_PatternPositionOccurrence_CorrectSql()
	{
		SqlCommand sql =
			SELECT(REGEXP_SUBSTR(_t.name, L("[abc]"), L(2), L(3)))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("REGEXP_SUBSTR(t.name, '[abc]', 2, 3)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void REGEXP_SUBSTR_PatternPositionOccurrenceOptions_CorrectSql()
	{
		SqlCommand sql =
			SELECT(REGEXP_SUBSTR(_t.name, L("[abc]"), L(2), L(3), RegexpOptions.CaseInsensitive))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("REGEXP_SUBSTR(t.name, '[abc]', 2, 3, 'i')");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void REGEXP_SUBSTR_PatternPositionOccurrenceOptionsSubPattern_CorrectSql()
	{
		SqlCommand sql =
			SELECT(REGEXP_SUBSTR(_t.name, L("[abc]"), L(2), L(3), RegexpOptions.None, L(1)))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("REGEXP_SUBSTR(t.name, '[abc]', 2, 3, '', 1)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void REPLACE_CharacterSearchAndReplacement_CorrectSql()
	{
		SqlCommand sql =
			SELECT(REPLACE(_t.name, L("a"), L("b")))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("REPLACE(t.name, 'a', 'b')");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void RPAD_CharacterLength_CorrectSql()
	{
		SqlCommand sql =
			SELECT(RPAD(_t.name, L(10)))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("RPAD(t.name, 10)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void RPAD_CharacterLengthPadding_CorrectSql()
	{
		SqlCommand sql =
			SELECT(RPAD(_t.name, L(10), L("a")))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("RPAD(t.name, 10, 'a')");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void RTRIM_Character_CorrectSql()
	{
		SqlCommand sql =
			SELECT(RTRIM(_t.name))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("RTRIM(t.name)");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void RTRIM_CharacterTrimChars_CorrectSql()
	{
		SqlCommand sql =
			SELECT(RTRIM(_t.name, L("a")))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("RTRIM(t.name, 'a')");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
