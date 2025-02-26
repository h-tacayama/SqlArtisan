using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class CaseTest
{
	private readonly test_table _t = new("t");

	[Fact]
	public void SELECT_SEARCH_CASE_Character_1()
	{
		SqlCommand sql =
			SELECT(
				CASE(
					WHEN(_t.name == L("a")).THEN(L("b")),
					ELSE(L("z"))))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("CASE");
		expected.AppendLine("WHEN (t.name = 'a') THEN 'b'");
		expected.AppendLine("ELSE 'z'");
		expected.Append("END");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_SEARCH_CASE_Character_2()
	{
		SqlCommand sql =
			SELECT(
				CASE(
					WHEN(_t.name == L("a")).THEN(L("b")),
					WHEN(_t.name == L("c")).THEN(L("d")),
					ELSE(L("z"))))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("CASE");
		expected.AppendLine("WHEN (t.name = 'a') THEN 'b'");
		expected.AppendLine("WHEN (t.name = 'c') THEN 'd'");
		expected.AppendLine("ELSE 'z'");
		expected.Append("END");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_SEARCH_CASE_Character_3()
	{
		SqlCommand sql =
			SELECT(
				CASE(
					WHEN(_t.name == L("a")).THEN(L("b")),
					WHEN(_t.name == L("c")).THEN(L("d")),
					WHEN(_t.name == L("e")).THEN(L("f")),
					ELSE(L("z"))))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("CASE");
		expected.AppendLine("WHEN (t.name = 'a') THEN 'b'");
		expected.AppendLine("WHEN (t.name = 'c') THEN 'd'");
		expected.AppendLine("WHEN (t.name = 'e') THEN 'f'");
		expected.AppendLine("ELSE 'z'");
		expected.Append("END");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_SEARCH_CASE_DateTime_1()
	{
		SqlCommand sql =
			SELECT(
				CASE(
					WHEN(_t.code == L(1)).THEN(TO_DATE("2001/02/03", "YYYY/MM/DD")),
					ELSE(_t.created_at)))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("CASE");
		expected.AppendLine("WHEN (t.code = 1) THEN TO_DATE('2001/02/03\', 'YYYY/MM/DD')");
		expected.AppendLine("ELSE t.created_at");
		expected.Append("END");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_SEARCH_CASE_DateTime_2()
	{
		SqlCommand sql =
			SELECT(
				CASE(
					WHEN(_t.code == L(1)).THEN(TO_DATE("2001/02/03", "YYYY/MM/DD")),
					WHEN(_t.code == L(2)).THEN(TO_DATE("2004/05/06", "YYYY/MM/DD")),
					ELSE(_t.created_at)))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("CASE");
		expected.AppendLine("WHEN (t.code = 1) THEN TO_DATE('2001/02/03', 'YYYY/MM/DD')");
		expected.AppendLine("WHEN (t.code = 2) THEN TO_DATE('2004/05/06', 'YYYY/MM/DD')");
		expected.AppendLine("ELSE t.created_at");
		expected.Append("END");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_SEARCH_CASE_DateTime_3()
	{
		SqlCommand sql =
			SELECT(
				CASE(
					WHEN(_t.code == L(1)).THEN(TO_DATE("2001/02/03", "YYYY/MM/DD")),
					WHEN(_t.code == L(2)).THEN(TO_DATE("2004/05/06", "YYYY/MM/DD")),
					WHEN(_t.code == L(3)).THEN(TO_DATE("2007/08/09", "YYYY/MM/DD")),
					ELSE(_t.created_at)))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("CASE");
		expected.AppendLine("WHEN (t.code = 1) THEN TO_DATE('2001/02/03', 'YYYY/MM/DD')");
		expected.AppendLine("WHEN (t.code = 2) THEN TO_DATE('2004/05/06', 'YYYY/MM/DD')");
		expected.AppendLine("WHEN (t.code = 3) THEN TO_DATE('2007/08/09', 'YYYY/MM/DD')");
		expected.AppendLine("ELSE t.created_at");
		expected.Append("END");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_SEARCH_CASE_Numeric_1()
	{
		SqlCommand sql =
			SELECT(
				CASE(
					WHEN(_t.code == L(1)).THEN(L(2)),
					ELSE(L(99))))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("CASE");
		expected.AppendLine("WHEN (t.code = 1) THEN 2");
		expected.AppendLine("ELSE 99");
		expected.Append("END");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_SEARCH_CASE_Numeric_2()
	{
		SqlCommand sql =
			SELECT(
				CASE(
					WHEN(_t.code == L(1)).THEN(L(2)),
					WHEN(_t.code == L(3)).THEN(L(4)),
					ELSE(L(99))))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("CASE");
		expected.AppendLine("WHEN (t.code = 1) THEN 2");
		expected.AppendLine("WHEN (t.code = 3) THEN 4");
		expected.AppendLine("ELSE 99");
		expected.Append("END");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_SEARCH_CASE_Numeric_3()
	{
		SqlCommand sql =
			SELECT(
				CASE(
					WHEN(_t.code == L(1)).THEN(L(2)),
					WHEN(_t.code == L(3)).THEN(L(4)),
					WHEN(_t.code == L(5)).THEN(L(6)),
					ELSE(L(99))))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("CASE");
		expected.AppendLine("WHEN (t.code = 1) THEN 2");
		expected.AppendLine("WHEN (t.code = 3) THEN 4");
		expected.AppendLine("WHEN (t.code = 5) THEN 6");
		expected.AppendLine("ELSE 99");
		expected.Append("END");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_SIMPLE_CASE_Character_1()
	{
		SqlCommand sql =
			SELECT(
				CASE(
					_t.code,
					WHEN(L(1)).THEN(L("a")),
					ELSE(L("z"))))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("CASE");
		expected.AppendLine("t.code");
		expected.AppendLine("WHEN 1 THEN 'a'");
		expected.AppendLine("ELSE 'z'");
		expected.Append("END");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_SIMPLE_CASE_Character_2()
	{
		SqlCommand sql =
			SELECT(
				CASE(
					_t.code,
					WHEN(L(1)).THEN(L("a")),
					WHEN(L(2)).THEN(L("b")),
					ELSE(L("z"))))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("CASE");
		expected.AppendLine("t.code");
		expected.AppendLine("WHEN 1 THEN 'a'");
		expected.AppendLine("WHEN 2 THEN 'b'");
		expected.AppendLine("ELSE 'z'");
		expected.Append("END");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_SIMPLE_CASE_Character_3()
	{
		SqlCommand sql =
			SELECT(
				CASE(
					_t.code,
					WHEN(L(1)).THEN(L("a")),
					WHEN(L(2)).THEN(L("b")),
					WHEN(L(3)).THEN(L("c")),
					ELSE(L("z"))))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("CASE");
		expected.AppendLine("t.code");
		expected.AppendLine("WHEN 1 THEN 'a'");
		expected.AppendLine("WHEN 2 THEN 'b'");
		expected.AppendLine("WHEN 3 THEN 'c'");
		expected.AppendLine("ELSE 'z'");
		expected.Append("END");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_SIMPLE_CASE_DateTime_1()
	{
		SqlCommand sql =
			SELECT(
				CASE(
					_t.name,
					WHEN(L("a")).THEN(TO_DATE("2001/02/03", "YYYY/MM/DD")),
					ELSE(_t.created_at)))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("CASE");
		expected.AppendLine("t.name");
		expected.AppendLine("WHEN 'a' THEN TO_DATE('2001/02/03', 'YYYY/MM/DD')");
		expected.AppendLine("ELSE t.created_at");
		expected.Append("END");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_SIMPLE_CASE_DateTime_2()
	{
		SqlCommand sql =
			SELECT(
				CASE(
					_t.name,
					WHEN(L("a")).THEN(TO_DATE("2001/02/03", "YYYY/MM/DD")),
					WHEN(L("b")).THEN(TO_DATE("2004/05/06", "YYYY/MM/DD")),
					ELSE(_t.created_at)))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("CASE");
		expected.AppendLine("t.name");
		expected.AppendLine("WHEN 'a' THEN TO_DATE('2001/02/03', 'YYYY/MM/DD')");
		expected.AppendLine("WHEN 'b' THEN TO_DATE('2004/05/06', 'YYYY/MM/DD')");
		expected.AppendLine("ELSE t.created_at");
		expected.Append("END");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_SIMPLE_CASE_DateTime_3()
	{
		SqlCommand sql =
			SELECT(
				CASE(
					_t.name,
					WHEN(L("a")).THEN(TO_DATE("2001/02/03", "YYYY/MM/DD")),
					WHEN(L("b")).THEN(TO_DATE("2004/05/06", "YYYY/MM/DD")),
					WHEN(L("c")).THEN(TO_DATE("2007/08/09", "YYYY/MM/DD")),
					ELSE(_t.created_at)))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("CASE");
		expected.AppendLine("t.name");
		expected.AppendLine("WHEN 'a' THEN TO_DATE('2001/02/03', 'YYYY/MM/DD')");
		expected.AppendLine("WHEN 'b' THEN TO_DATE('2004/05/06', 'YYYY/MM/DD')");
		expected.AppendLine("WHEN 'c' THEN TO_DATE('2007/08/09', 'YYYY/MM/DD')");
		expected.AppendLine("ELSE t.created_at");
		expected.Append("END");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_SIMPLE_CASE_Numeric_1()
	{
		SqlCommand sql =
			SELECT(
				CASE(
					_t.name,
					WHEN(L("a")).THEN(L(1)),
					ELSE(L(99))))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("CASE");
		expected.AppendLine("t.name");
		expected.AppendLine("WHEN 'a' THEN 1");
		expected.AppendLine("ELSE 99");
		expected.Append("END");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_SIMPLE_CASE_Numeric_2()
	{
		SqlCommand sql =
			SELECT(
				CASE(
					_t.name,
					WHEN(L("a")).THEN(L(1)),
					WHEN(L("b")).THEN(L(2)),
					ELSE(L(99))))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("CASE");
		expected.AppendLine("t.name");
		expected.AppendLine("WHEN 'a' THEN 1");
		expected.AppendLine("WHEN 'b' THEN 2");
		expected.AppendLine("ELSE 99");
		expected.Append("END");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void SELECT_SIMPLE_CASE_Numeric_3()
	{
		SqlCommand sql =
			SELECT(
				CASE(
					_t.name,
					WHEN(L("a")).THEN(L(1)),
					WHEN(L("b")).THEN(L(2)),
					WHEN(L("c")).THEN(L(3)),
					ELSE(L(99))))
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("CASE");
		expected.AppendLine("t.name");
		expected.AppendLine("WHEN 'a' THEN 1");
		expected.AppendLine("WHEN 'b' THEN 2");
		expected.AppendLine("WHEN 'c' THEN 3");
		expected.AppendLine("ELSE 99");
		expected.Append("END");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
