using System.Text;
using static InlineSqlSharp.Oracle.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class OrderByTest
{
	private test_table _t = new("t");

	[Fact]
	public void ORDER_BY_Character()
	{
		SqlCommand sql =
			SELECT(_t.name)
			.FROM(_t)
			.ORDER_BY(
				_t.name,
				_t.name.ASC,
				_t.name.DESC,
				_t.name.NULLS_FIRST,
				_t.name.ASC.NULLS_FIRST,
				_t.name.DESC.NULLS_FIRST,
				_t.name.NULLS_LAST,
				_t.name.ASC.NULLS_LAST,
				_t.name.DESC.NULLS_LAST)
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("t.name");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table t");
		expected.AppendLine("ORDER BY");
		expected.AppendLine("t.name");
		expected.AppendLine(", t.name ASC");
		expected.AppendLine(", t.name DESC");
		expected.AppendLine(", t.name NULLS FIRST");
		expected.AppendLine(", t.name ASC NULLS FIRST");
		expected.AppendLine(", t.name DESC NULLS FIRST");
		expected.AppendLine(", t.name NULLS LAST");
		expected.AppendLine(", t.name ASC NULLS LAST");
		expected.Append(", t.name DESC NULLS LAST");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void ORDER_BY_DateTime()
	{
		SqlCommand sql =
			SELECT(_t.created_at)
			.FROM(_t)
			.ORDER_BY(
				_t.created_at,
				_t.created_at.ASC,
				_t.created_at.DESC,
				_t.created_at.NULLS_FIRST,
				_t.created_at.ASC.NULLS_FIRST,
				_t.created_at.DESC.NULLS_FIRST,
				_t.created_at.NULLS_LAST,
				_t.created_at.ASC.NULLS_LAST,
				_t.created_at.DESC.NULLS_LAST)
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("t.created_at");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table t");
		expected.AppendLine("ORDER BY");
		expected.AppendLine("t.created_at");
		expected.AppendLine(", t.created_at ASC");
		expected.AppendLine(", t.created_at DESC");
		expected.AppendLine(", t.created_at NULLS FIRST");
		expected.AppendLine(", t.created_at ASC NULLS FIRST");
		expected.AppendLine(", t.created_at DESC NULLS FIRST");
		expected.AppendLine(", t.created_at NULLS LAST");
		expected.AppendLine(", t.created_at ASC NULLS LAST");
		expected.Append(", t.created_at DESC NULLS LAST");

		Assert.Equal(expected.ToString(), sql.Statement);
	}

	[Fact]
	public void ORDER_BY_Numeric()
	{
		SqlCommand sql =
			SELECT(_t.code)
			.FROM(_t)
			.ORDER_BY(
				_t.code,
				_t.code.ASC,
				_t.code.DESC,
				_t.code.NULLS_FIRST,
				_t.code.ASC.NULLS_FIRST,
				_t.code.DESC.NULLS_FIRST,
				_t.code.NULLS_LAST,
				_t.code.ASC.NULLS_LAST,
				_t.code.DESC.NULLS_LAST)
			.Build();

		StringBuilder expected = new();
		expected.AppendLine("SELECT");
		expected.AppendLine("t.code");
		expected.AppendLine("FROM");
		expected.AppendLine("test_table t");
		expected.AppendLine("ORDER BY");
		expected.AppendLine("t.code");
		expected.AppendLine(", t.code ASC");
		expected.AppendLine(", t.code DESC");
		expected.AppendLine(", t.code NULLS FIRST");
		expected.AppendLine(", t.code ASC NULLS FIRST");
		expected.AppendLine(", t.code DESC NULLS FIRST");
		expected.AppendLine(", t.code NULLS LAST");
		expected.AppendLine(", t.code ASC NULLS LAST");
		expected.Append(", t.code DESC NULLS LAST");

		Assert.Equal(expected.ToString(), sql.Statement);
	}
}
