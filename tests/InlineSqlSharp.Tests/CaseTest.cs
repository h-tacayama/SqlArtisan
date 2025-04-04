using System.Text;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class CaseTest
{
	private readonly test_table _t = new("t");

	[Fact]
	public void CASE_SearchCaseWithCharacter_CorrectSql()
	{
		SqlStatement sql =
			SELECT(
				CASE(
					[
						WHEN(_t.name == L("a")).THEN(L("b")),
						WHEN(_t.name == L("c")).THEN(L("d")),
					],
					ELSE(L("z"))))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("CASE ");
		expected.Append("WHEN (\"t\".name = 'a') THEN 'b' ");
		expected.Append("WHEN (\"t\".name = 'c') THEN 'd' ");
		expected.Append("ELSE 'z' ");
		expected.Append("END");

		Assert.Equal(expected.ToString(), sql.Text);
	}

	[Fact]
	public void CASE_SearchCaseWithDateTime_CorrectSql()
	{
		SqlStatement sql =
			SELECT(
				CASE(
					[
						WHEN(_t.code == L(1)).THEN(TO_DATE(L("2001/02/03"), L("YYYY/MM/DD"))),
						WHEN(_t.code == L(2)).THEN(TO_DATE(L("2004/05/06"), L("YYYY/MM/DD"))),
					],
					ELSE(_t.created_at)))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("CASE ");
		expected.Append("WHEN (\"t\".code = 1) THEN TO_DATE('2001/02/03', 'YYYY/MM/DD') ");
		expected.Append("WHEN (\"t\".code = 2) THEN TO_DATE('2004/05/06', 'YYYY/MM/DD') ");
		expected.Append("ELSE \"t\".created_at ");
		expected.Append("END");

		Assert.Equal(expected.ToString(), sql.Text);
	}

	[Fact]
	public void CASE_SearchCaseWithNumeric_CorrectSql()
	{
		SqlStatement sql =
			SELECT(
				CASE(
					[
						WHEN(_t.code == L(1)).THEN(L(2)),
						WHEN(_t.code == L(3)).THEN(L(4)),
					],
					ELSE(L(99))))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("CASE ");
		expected.Append("WHEN (\"t\".code = 1) THEN 2 ");
		expected.Append("WHEN (\"t\".code = 3) THEN 4 ");
		expected.Append("ELSE 99 ");
		expected.Append("END");

		Assert.Equal(expected.ToString(), sql.Text);
	}

	[Fact]
	public void CASE_SimpleCaseWithCharacter_CorrectSql()
	{
		SqlStatement sql =
			SELECT(
				CASE(
					_t.code,
					[
						WHEN(L(1)).THEN(L("a")),
						WHEN(L(2)).THEN(L("b")),
					],
					ELSE(L("z"))))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("CASE ");
		expected.Append("\"t\".code ");
		expected.Append("WHEN 1 THEN 'a' ");
		expected.Append("WHEN 2 THEN 'b' ");
		expected.Append("ELSE 'z' ");
		expected.Append("END");

		Assert.Equal(expected.ToString(), sql.Text);
	}

	[Fact]
	public void CASE_SimpleCaseWithDateTime_CorrectSql()
	{
		SqlStatement sql =
			SELECT(
				CASE(
					_t.name,
					[
						WHEN(L("a")).THEN(TO_DATE(L("2001/02/03"), L("YYYY/MM/DD"))),
						WHEN(L("b")).THEN(TO_DATE(L("2004/05/06"), L("YYYY/MM/DD"))),
					],
					ELSE(_t.created_at)))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("CASE ");
		expected.Append("\"t\".name ");
		expected.Append("WHEN 'a' THEN TO_DATE('2001/02/03', 'YYYY/MM/DD') ");
		expected.Append("WHEN 'b' THEN TO_DATE('2004/05/06', 'YYYY/MM/DD') ");
		expected.Append("ELSE \"t\".created_at ");
		expected.Append("END");

		Assert.Equal(expected.ToString(), sql.Text);
	}

	[Fact]
	public void CASE_SimpleCaseWithNumeric_CorrectSql()
	{
		SqlStatement sql =
			SELECT(
				CASE(
					_t.name,
					[
						WHEN(L("a")).THEN(L(1)),
						WHEN(L("b")).THEN(L(2)),
					],
					ELSE(L(99))))
			.Build();

		StringBuilder expected = new();
		expected.Append("SELECT ");
		expected.Append("CASE ");
		expected.Append("\"t\".name ");
		expected.Append("WHEN 'a' THEN 1 ");
		expected.Append("WHEN 'b' THEN 2 ");
		expected.Append("ELSE 99 ");
		expected.Append("END");

		Assert.Equal(expected.ToString(), sql.Text);
	}
}
