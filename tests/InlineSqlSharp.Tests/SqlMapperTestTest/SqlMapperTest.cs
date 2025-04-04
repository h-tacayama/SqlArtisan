using System.Data;
using InlineSqlSharp.Dapper;
using static Dapper.SqlMapper;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

#if SQL_MAPPER_TEST
public class SqlMapperTest : AbstractSqlMapperTest
{
	private readonly test_table _t = new("t");

	[Fact]
	public void ExecuteScalar_NoTypeParameter_ReturnsObjectValue()
	{
		ISqlBuilder sql =
			SELECT(_t.code)
			.FROM(_t)
			.WHERE(_t.code == P(1));

		object? result = _conn.ExecuteScalar(sql);

		Assert.Equal(1, result);
	}

	[Fact]
	public void ExecuteScalar_IntTypeParameter_ReturnsIntValue()
	{
		ISqlBuilder sql =
			SELECT(_t.code)
			.FROM(_t)
			.WHERE(_t.code == P(1));

		int result = _conn.ExecuteScalar<int>(sql);

		Assert.Equal(1, result);
	}

	[Fact]
	public void ExecuteScalar_StringTypeParameter_ReturnsStringValue()
	{
		ISqlBuilder sql =
			SELECT(_t.name)
			.FROM(_t)
			.WHERE(_t.code == P(1));

		string? result = _conn.ExecuteScalar<string>(sql);

		Assert.Equal("Test1", result);
	}

	[Fact]
	public void ExecuteScalar_DateTimeTypeParameter_ReturnsDateTimeValue()
	{
		ISqlBuilder sql =
			SELECT(_t.created_at)
			.FROM(_t)
			.WHERE(_t.code == P(1));

		DateTime result = _conn.ExecuteScalar<DateTime>(sql);

		Assert.Equal(new DateTime(2025, 4, 1), result);
	}

	[Fact]
	public void QuerySingle_TestTableDtoType_ReturnsObjectValue()
	{
		ISqlBuilder sql =
			SELECT(
				_t.code,
				_t.name,
				_t.created_at)
			.FROM(_t)
			.WHERE(_t.code == P(1));

		object result = _conn.QuerySingle(typeof(TestTableDto), sql);
		TestTableDto dto = ((TestTableDto)result);

		Assert.Equal(1, dto.Code);
		Assert.Equal("Test1", dto.Name);
		Assert.Equal(new DateTime(2025, 4, 1), dto.CreatedAt);
	}

	[Fact]
	public void QuerySingle_NoType_ReturnsDynamicValue()
	{
		ISqlBuilder sql =
			SELECT(
				_t.code,
				_t.name,
				_t.created_at)
			.FROM(_t)
			.WHERE(_t.code == P(1));

		dynamic result = _conn.QuerySingle(sql);
		IDictionary<string, object> row = (IDictionary<string, object>)result;

		Assert.Equal(1, row["CODE"]);
		Assert.Equal("Test1", row["NAME"]);
		Assert.Equal(new DateTime(2025, 4, 1), row["CREATED_AT"]);
	}

	[Fact]
	public void QuerySingle_TestTableDtoType_ReturnsTestTableDtoValue()
	{
		ISqlBuilder sql =
			SELECT(
				_t.code,
				_t.name,
				_t.created_at)
			.FROM(_t)
			.WHERE(_t.code == P(1));

		TestTableDto result = _conn.QuerySingle<TestTableDto>(sql);

		Assert.Equal(1, result.Code);
		Assert.Equal("Test1", result.Name);
		Assert.Equal(new DateTime(2025, 4, 1), result.CreatedAt);
	}

	[Fact]
	public void QuerySingleOrDefault_TestTableDtoType_ReturnsObjectValue()
	{
		ISqlBuilder sql =
			SELECT(
				_t.code,
				_t.name,
				_t.created_at)
			.FROM(_t)
			.WHERE(_t.code == P(1));

		object? result = _conn.QuerySingleOrDefault(typeof(TestTableDto), sql);
		TestTableDto? dto = result as TestTableDto;

		Assert.Equal(1, dto?.Code);
		Assert.Equal("Test1", dto?.Name);
		Assert.Equal(new DateTime(2025, 4, 1), dto?.CreatedAt);
	}

	[Fact]
	public void QuerySingleOrDefault_NoMatch_ReturnsNull()
	{
		ISqlBuilder sql =
			SELECT(
				_t.code,
				_t.name,
				_t.created_at)
			.FROM(_t)
			.WHERE(_t.code == P(999));

		object? result = _conn.QuerySingleOrDefault(typeof(TestTableDto), sql);

		Assert.Null(result);
	}

	[Fact]
	public void QuerySingleOrDefault_NoType_ReturnsDynamicValue()
	{
		ISqlBuilder sql =
			SELECT(
				_t.code,
				_t.name,
				_t.created_at)
			.FROM(_t)
			.WHERE(_t.code == P(1));

		dynamic? result = _conn.QuerySingleOrDefault(sql);
		IDictionary<string, object>? row = result as IDictionary<string, object>;

		Assert.Equal(1, row?["CODE"]);
		Assert.Equal("Test1", row?["NAME"]);
		Assert.Equal(new DateTime(2025, 4, 1), row?["CREATED_AT"]);
	}

	[Fact]
	public void QuerySingleOrDefault_NoTypeNoMatch_ReturnsNull()
	{
		ISqlBuilder sql =
			SELECT(
				_t.code,
				_t.name,
				_t.created_at)
			.FROM(_t)
			.WHERE(_t.code == P(999));

		dynamic? result = _conn.QuerySingleOrDefault(sql);
		IDictionary<string, object>? row = result as IDictionary<string, object>;

		Assert.Null(row);
	}

	[Fact]
	public void QuerySingleOrDefault_TestTableDtoType_ReturnsTestTableDtoValue()
	{
		ISqlBuilder sql =
			SELECT(
				_t.code,
				_t.name,
				_t.created_at)
			.FROM(_t)
			.WHERE(_t.code == P(1));

		TestTableDto? result = _conn.QuerySingleOrDefault<TestTableDto>(sql);

		Assert.Equal(1, result?.Code);
		Assert.Equal("Test1", result?.Name);
		Assert.Equal(new DateTime(2025, 4, 1), result?.CreatedAt);
	}

	[Fact]
	public void QuerySingleOrDefault_TestTableDtoTypeNoMatch_ReturnsNull()
	{
		ISqlBuilder sql =
			SELECT(
				_t.code,
				_t.name,
				_t.created_at)
			.FROM(_t)
			.WHERE(_t.code == P(999));

		TestTableDto? result = _conn.QuerySingleOrDefault<TestTableDto>(sql);

		Assert.Null(result);
	}

	[Fact]
	public void QueryFirst_TestTableDtoType_ReturnsObjectValue()
	{
		ISqlBuilder sql =
			SELECT(
				_t.code,
				_t.name,
				_t.created_at)
			.FROM(_t)
			.WHERE(_t.code == P(1));

		object result = _conn.QueryFirst(typeof(TestTableDto), sql);
		TestTableDto dto = ((TestTableDto)result);

		Assert.Equal(1, dto.Code);
		Assert.Equal("Test1", dto.Name);
		Assert.Equal(new DateTime(2025, 4, 1), dto.CreatedAt);
	}

	[Fact]
	public void QueryFirst_NoType_ReturnsDynamicValue()
	{
		ISqlBuilder sql =
			SELECT(
				_t.code,
				_t.name,
				_t.created_at)
			.FROM(_t)
			.WHERE(_t.code == P(1));

		dynamic result = _conn.QueryFirst(sql);
		IDictionary<string, object> row = (IDictionary<string, object>)result;

		Assert.Equal(1, row["CODE"]);
		Assert.Equal("Test1", row["NAME"]);
		Assert.Equal(new DateTime(2025, 4, 1), row["CREATED_AT"]);
	}

	[Fact]
	public void QueryFirst_TestTableDtoType_ReturnsTestTableDtoValue()
	{
		ISqlBuilder sql =
			SELECT(
				_t.code,
				_t.name,
				_t.created_at)
			.FROM(_t)
			.WHERE(_t.code == P(1));

		TestTableDto result = _conn.QueryFirst<TestTableDto>(sql);

		Assert.Equal(1, result.Code);
		Assert.Equal("Test1", result.Name);
		Assert.Equal(new DateTime(2025, 4, 1), result.CreatedAt);
	}

	[Fact]
	public void QueryFirstOrDefault_TestTableDtoType_ReturnsObjectValue()
	{
		ISqlBuilder sql =
			SELECT(
				_t.code,
				_t.name,
				_t.created_at)
			.FROM(_t)
			.WHERE(_t.code == P(1));

		object? result = _conn.QueryFirstOrDefault(typeof(TestTableDto), sql);
		TestTableDto? dto = result as TestTableDto;

		Assert.Equal(1, dto?.Code);
		Assert.Equal("Test1", dto?.Name);
		Assert.Equal(new DateTime(2025, 4, 1), dto?.CreatedAt);
	}

	[Fact]
	public void QueryFirstOrDefault_NoMatch_ReturnsNull()
	{
		ISqlBuilder sql =
			SELECT(
				_t.code,
				_t.name,
				_t.created_at)
			.FROM(_t)
			.WHERE(_t.code == P(999));

		object? result = _conn.QueryFirstOrDefault(typeof(TestTableDto), sql);

		Assert.Null(result);
	}

	[Fact]
	public void QueryFirstOrDefault_NoType_ReturnsDynamicValue()
	{
		ISqlBuilder sql =
			SELECT(
				_t.code,
				_t.name,
				_t.created_at)
			.FROM(_t)
			.WHERE(_t.code == P(1));

		dynamic? result = _conn.QueryFirstOrDefault(sql);
		IDictionary<string, object>? row = result as IDictionary<string, object>;

		Assert.Equal(1, row?["CODE"]);
		Assert.Equal("Test1", row?["NAME"]);
		Assert.Equal(new DateTime(2025, 4, 1), row?["CREATED_AT"]);
	}

	[Fact]
	public void QueryFirstOrDefault_NoTypeNoMatch_ReturnsNull()
	{
		ISqlBuilder sql =
			SELECT(
				_t.code,
				_t.name,
				_t.created_at)
			.FROM(_t)
			.WHERE(_t.code == P(999));

		dynamic? result = _conn.QueryFirstOrDefault(sql);
		IDictionary<string, object>? row = result as IDictionary<string, object>;

		Assert.Null(row);
	}

	[Fact]
	public void QueryFirstOrDefault_TestTableDtoType_ReturnsTestTableDtoValue()
	{
		ISqlBuilder sql =
			SELECT(
				_t.code,
				_t.name,
				_t.created_at)
			.FROM(_t)
			.WHERE(_t.code == P(1));

		TestTableDto? result = _conn.QueryFirstOrDefault<TestTableDto>(sql);

		Assert.Equal(1, result?.Code);
		Assert.Equal("Test1", result?.Name);
		Assert.Equal(new DateTime(2025, 4, 1), result?.CreatedAt);
	}

	[Fact]
	public void QueryFirstOrDefault_TestTableDtoTypeNoMatch_ReturnsNull()
	{
		ISqlBuilder sql =
			SELECT(
				_t.code,
				_t.name,
				_t.created_at)
			.FROM(_t)
			.WHERE(_t.code == P(999));

		TestTableDto? result = _conn.QueryFirstOrDefault<TestTableDto>(sql);

		Assert.Null(result);
	}

	[Fact]
	public void Query_TestTableDtoType_ReturnsObjectCollection()
	{
		ISqlBuilder sql =
			SELECT(
				_t.code,
				_t.name,
				_t.created_at)
			.FROM(_t)
			.WHERE(_t.code >= P(1));

		IEnumerable<object> result = _conn.Query(typeof(TestTableDto), sql);

		Assert.Equal(2, result.Count());
	}

	[Fact]
	public void Query_NoType_ReturnsDynamicCollection()
	{
		ISqlBuilder sql =
			SELECT(
				_t.code,
				_t.name,
				_t.created_at)
			.FROM(_t)
			.WHERE(_t.code >= P(1));

		IEnumerable<dynamic> result = _conn.Query(sql);

		Assert.Equal(2, result.Count());
	}

	[Fact]
	public void Query_TestTableDtoType_ReturnsTestTableDtoCollection()
	{
		ISqlBuilder sql =
			SELECT(
				_t.code,
				_t.name,
				_t.created_at)
			.FROM(_t)
			.WHERE(_t.code >= P(1));

		IEnumerable<TestTableDto> result = _conn.Query<TestTableDto>(sql);

		Assert.Equal(2, result.Count());
	}

	[Fact]
	public void QueryMultiple_MatchesTwoRecords_ReturnsGridReader()
	{
		ISqlBuilder sql =
			SELECT(
				_t.code,
				_t.name,
				_t.created_at)
			.FROM(_t)
			.WHERE(_t.code >= P(1));

		using (GridReader result = _conn.QueryMultiple(sql))
		{
			IEnumerable<dynamic> rows = result.Read();
			Assert.Equal(2, rows.Count());
		}
	}

	[Fact]
	public void ExecuteReader_MatchesTwoRecords_ReturnsIDataReader()
	{
		ISqlBuilder sql =
			SELECT(
				_t.code,
				_t.name,
				_t.created_at)
			.FROM(_t)
			.WHERE(_t.code >= P(1));

		using (IDataReader result = _conn.ExecuteReader(sql))
		{
			Assert.True(result.Read());
			Assert.True(result.Read());
			Assert.False(result.Read());
		}
	}
}
#endif
