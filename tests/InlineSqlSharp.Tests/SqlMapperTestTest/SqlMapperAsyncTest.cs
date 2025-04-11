#if SQL_MAPPER_TEST
using System.Data;
using InlineSqlSharp.Dapper;
using static Dapper.SqlMapper;
using static InlineSqlSharp.SqlWordbook;

namespace InlineSqlSharp.Tests;

public class SqlMapperAsyncTest : AbstractSqlMapperTest
{
    private readonly test_table _t = new("t");

    [Fact]
    public async void ExecuteScalarAsync_NoTypeParameter_ReturnsObjectValue()
    {
        ISqlBuilder sql =
            SELECT(_t.code)
            .FROM(_t)
            .WHERE(_t.code == P(1));

        object? result = await _conn.ExecuteScalarAsync(sql);

        Assert.Equal(1, result);
    }

    [Fact]
    public async void ExecuteScalarAsync_IntTypeParameter_ReturnsIntValue()
    {
        ISqlBuilder sql =
            SELECT(_t.code)
            .FROM(_t)
            .WHERE(_t.code == P(1));

        int result = await _conn.ExecuteScalarAsync<int>(sql);

        Assert.Equal(1, result);
    }

    [Fact]
    public async void ExecuteScalarAsync_StringTypeParameter_ReturnsStringValue()
    {
        ISqlBuilder sql =
            SELECT(_t.name)
            .FROM(_t)
            .WHERE(_t.code == P(1));

        string? result = await _conn.ExecuteScalarAsync<string>(sql);

        Assert.Equal("Test1", result);
    }

    [Fact]
    public async void ExecuteScalarAsync_DateTimeTypeParameter_ReturnsDateTimeValue()
    {
        ISqlBuilder sql =
            SELECT(_t.created_at)
            .FROM(_t)
            .WHERE(_t.code == P(1));

        DateTime result = await _conn.ExecuteScalarAsync<DateTime>(sql);

        Assert.Equal(new DateTime(2025, 4, 1), result);
    }

    [Fact]
    public async void QuerySingleAsync_TestTableDtoType_ReturnsObjectValue()
    {
        ISqlBuilder sql =
            SELECT(
                _t.code,
                _t.name,
                _t.created_at)
            .FROM(_t)
            .WHERE(_t.code == P(1));

        object result = await _conn.QuerySingleAsync(typeof(TestTableDto), sql);
        TestTableDto dto = ((TestTableDto)result);

        Assert.Equal(1, dto.Code);
        Assert.Equal("Test1", dto.Name);
        Assert.Equal(new DateTime(2025, 4, 1), dto.CreatedAt);
    }

    [Fact]
    public async void QuerySingleAsync_NoType_ReturnsDynamicValue()
    {
        ISqlBuilder sql =
            SELECT(
                _t.code,
                _t.name,
                _t.created_at)
            .FROM(_t)
            .WHERE(_t.code == P(1));

        dynamic result = await _conn.QuerySingleAsync(sql);
        IDictionary<string, object> row = (IDictionary<string, object>)result;

        Assert.Equal(1, row["CODE"]);
        Assert.Equal("Test1", row["NAME"]);
        Assert.Equal(new DateTime(2025, 4, 1), row["CREATED_AT"]);
    }

    [Fact]
    public async void QuerySingleAsync_TestTableDtoType_ReturnsTestTableDtoValue()
    {
        ISqlBuilder sql =
            SELECT(
                _t.code,
                _t.name,
                _t.created_at)
            .FROM(_t)
            .WHERE(_t.code == P(1));

        TestTableDto result = await _conn.QuerySingleAsync<TestTableDto>(sql);

        Assert.Equal(1, result.Code);
        Assert.Equal("Test1", result.Name);
        Assert.Equal(new DateTime(2025, 4, 1), result.CreatedAt);
    }

    [Fact]
    public async void QuerySingleOrDefaultAsync_TestTableDtoType_ReturnsObjectValue()
    {
        ISqlBuilder sql =
            SELECT(
                _t.code,
                _t.name,
                _t.created_at)
            .FROM(_t)
            .WHERE(_t.code == P(1));

        object? result = await _conn.QuerySingleOrDefaultAsync(typeof(TestTableDto), sql);
        TestTableDto? dto = result as TestTableDto;

        Assert.Equal(1, dto?.Code);
        Assert.Equal("Test1", dto?.Name);
        Assert.Equal(new DateTime(2025, 4, 1), dto?.CreatedAt);
    }

    [Fact]
    public async void QuerySingleOrDefaultAsync_NoMatch_ReturnsNull()
    {
        ISqlBuilder sql =
            SELECT(
                _t.code,
                _t.name,
                _t.created_at)
            .FROM(_t)
            .WHERE(_t.code == P(999));

        object? result = await _conn.QuerySingleOrDefaultAsync(typeof(TestTableDto), sql);

        Assert.Null(result);
    }

    [Fact]
    public async void QuerySingleOrDefaultAsync_NoType_ReturnsDynamicValue()
    {
        ISqlBuilder sql =
            SELECT(
                _t.code,
                _t.name,
                _t.created_at)
            .FROM(_t)
            .WHERE(_t.code == P(1));

        dynamic? result = await _conn.QuerySingleOrDefaultAsync(sql);
        IDictionary<string, object>? row = result as IDictionary<string, object>;

        Assert.Equal(1, row?["CODE"]);
        Assert.Equal("Test1", row?["NAME"]);
        Assert.Equal(new DateTime(2025, 4, 1), row?["CREATED_AT"]);
    }

    [Fact]
    public async void QuerySingleOrDefaultAsync_NoTypeNoMatch_ReturnsNull()
    {
        ISqlBuilder sql =
            SELECT(
                _t.code,
                _t.name,
                _t.created_at)
            .FROM(_t)
            .WHERE(_t.code == P(999));

        dynamic? result = await _conn.QuerySingleOrDefaultAsync(sql);
        IDictionary<string, object>? row = result as IDictionary<string, object>;

        Assert.Null(row);
    }

    [Fact]
    public async void QuerySingleOrDefaultAsync_TestTableDtoType_ReturnsTestTableDtoValue()
    {
        ISqlBuilder sql =
            SELECT(
                _t.code,
                _t.name,
                _t.created_at)
            .FROM(_t)
            .WHERE(_t.code == P(1));

        TestTableDto? result = await _conn.QuerySingleOrDefaultAsync<TestTableDto>(sql);

        Assert.Equal(1, result?.Code);
        Assert.Equal("Test1", result?.Name);
        Assert.Equal(new DateTime(2025, 4, 1), result?.CreatedAt);
    }

    [Fact]
    public async void QuerySingleOrDefaultAsync_TestTableDtoTypeNoMatch_ReturnsNull()
    {
        ISqlBuilder sql =
            SELECT(
                _t.code,
                _t.name,
                _t.created_at)
            .FROM(_t)
            .WHERE(_t.code == P(999));

        TestTableDto? result = await _conn.QuerySingleOrDefaultAsync<TestTableDto>(sql);

        Assert.Null(result);
    }

    [Fact]
    public async void QueryFirstAsync_TestTableDtoType_ReturnsObjectValue()
    {
        ISqlBuilder sql =
            SELECT(
                _t.code,
                _t.name,
                _t.created_at)
            .FROM(_t)
            .WHERE(_t.code == P(1));

        object result = await _conn.QueryFirstAsync(typeof(TestTableDto), sql);
        TestTableDto dto = ((TestTableDto)result);

        Assert.Equal(1, dto.Code);
        Assert.Equal("Test1", dto.Name);
        Assert.Equal(new DateTime(2025, 4, 1), dto.CreatedAt);
    }

    [Fact]
    public async void QueryFirstAsync_NoType_ReturnsDynamicValue()
    {
        ISqlBuilder sql =
            SELECT(
                _t.code,
                _t.name,
                _t.created_at)
            .FROM(_t)
            .WHERE(_t.code == P(1));

        dynamic result = await _conn.QueryFirstAsync(sql);
        IDictionary<string, object> row = (IDictionary<string, object>)result;

        Assert.Equal(1, row["CODE"]);
        Assert.Equal("Test1", row["NAME"]);
        Assert.Equal(new DateTime(2025, 4, 1), row["CREATED_AT"]);
    }

    [Fact]
    public async void QueryFirstAsync_TestTableDtoType_ReturnsTestTableDtoValue()
    {
        ISqlBuilder sql =
            SELECT(
                _t.code,
                _t.name,
                _t.created_at)
            .FROM(_t)
            .WHERE(_t.code == P(1));

        TestTableDto result = await _conn.QueryFirstAsync<TestTableDto>(sql);

        Assert.Equal(1, result.Code);
        Assert.Equal("Test1", result.Name);
        Assert.Equal(new DateTime(2025, 4, 1), result.CreatedAt);
    }

    [Fact]
    public async void QueryFirstOrDefaultAsync_TestTableDtoType_ReturnsObjectValue()
    {
        ISqlBuilder sql =
            SELECT(
                _t.code,
                _t.name,
                _t.created_at)
            .FROM(_t)
            .WHERE(_t.code == P(1));

        object? result = await _conn.QueryFirstOrDefaultAsync(typeof(TestTableDto), sql);
        TestTableDto? dto = result as TestTableDto;

        Assert.Equal(1, dto?.Code);
        Assert.Equal("Test1", dto?.Name);
        Assert.Equal(new DateTime(2025, 4, 1), dto?.CreatedAt);
    }

    [Fact]
    public async void QueryFirstOrDefaultAsync_NoMatch_ReturnsNull()
    {
        ISqlBuilder sql =
            SELECT(
                _t.code,
                _t.name,
                _t.created_at)
            .FROM(_t)
            .WHERE(_t.code == P(999));

        object? result = await _conn.QueryFirstOrDefaultAsync(typeof(TestTableDto), sql);

        Assert.Null(result);
    }

    [Fact]
    public async void QueryFirstOrDefaultAsync_NoType_ReturnsDynamicValue()
    {
        ISqlBuilder sql =
            SELECT(
                _t.code,
                _t.name,
                _t.created_at)
            .FROM(_t)
            .WHERE(_t.code == P(1));

        dynamic? result = await _conn.QueryFirstOrDefaultAsync(sql);
        IDictionary<string, object>? row = result as IDictionary<string, object>;

        Assert.Equal(1, row?["CODE"]);
        Assert.Equal("Test1", row?["NAME"]);
        Assert.Equal(new DateTime(2025, 4, 1), row?["CREATED_AT"]);
    }

    [Fact]
    public async void QueryFirstOrDefaultAsync_NoTypeNoMatch_ReturnsNull()
    {
        ISqlBuilder sql =
            SELECT(
                _t.code,
                _t.name,
                _t.created_at)
            .FROM(_t)
            .WHERE(_t.code == P(999));

        dynamic? result = await _conn.QueryFirstOrDefaultAsync(sql);
        IDictionary<string, object>? row = result as IDictionary<string, object>;

        Assert.Null(row);
    }

    [Fact]
    public async void QueryFirstOrDefaultAsync_TestTableDtoType_ReturnsTestTableDtoValue()
    {
        ISqlBuilder sql =
            SELECT(
                _t.code,
                _t.name,
                _t.created_at)
            .FROM(_t)
            .WHERE(_t.code == P(1));

        TestTableDto? result = await _conn.QueryFirstOrDefaultAsync<TestTableDto>(sql);

        Assert.Equal(1, result?.Code);
        Assert.Equal("Test1", result?.Name);
        Assert.Equal(new DateTime(2025, 4, 1), result?.CreatedAt);
    }

    [Fact]
    public async void QueryFirstOrDefaultAsync_TestTableDtoTypeNoMatch_ReturnsNull()
    {
        ISqlBuilder sql =
            SELECT(
                _t.code,
                _t.name,
                _t.created_at)
            .FROM(_t)
            .WHERE(_t.code == P(999));

        TestTableDto? result = await _conn.QueryFirstOrDefaultAsync<TestTableDto>(sql);

        Assert.Null(result);
    }

    [Fact]
    public async void QueryAsync_TestTableDtoType_ReturnsObjectCollection()
    {
        ISqlBuilder sql =
            SELECT(
                _t.code,
                _t.name,
                _t.created_at)
            .FROM(_t)
            .WHERE(_t.code >= P(1));

        IEnumerable<object> result = await _conn.QueryAsync(typeof(TestTableDto), sql);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async void QueryAsync_NoType_ReturnsDynamicCollection()
    {
        ISqlBuilder sql =
            SELECT(
                _t.code,
                _t.name,
                _t.created_at)
            .FROM(_t)
            .WHERE(_t.code >= P(1));

        IEnumerable<dynamic> result = await _conn.QueryAsync(sql);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async void QueryAsync_TestTableDtoType_ReturnsTestTableDtoCollection()
    {
        ISqlBuilder sql =
            SELECT(
                _t.code,
                _t.name,
                _t.created_at)
            .FROM(_t)
            .WHERE(_t.code >= P(1));

        IEnumerable<TestTableDto> result = await _conn.QueryAsync<TestTableDto>(sql);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async void QueryMultipleAsync_MatchesTwoRecords_ReturnsGridReader()
    {
        ISqlBuilder sql =
            SELECT(
                _t.code,
                _t.name,
                _t.created_at)
            .FROM(_t)
            .WHERE(_t.code >= P(1));

        using (GridReader result = await _conn.QueryMultipleAsync(sql))
        {
            IEnumerable<dynamic> rows = result.Read();
            Assert.Equal(2, rows.Count());
        }
    }

    [Fact]
    public async void ExecuteReaderAsync_MatchesTwoRecords_ReturnsIDataReader()
    {
        ISqlBuilder sql =
            SELECT(
                _t.code,
                _t.name,
                _t.created_at)
            .FROM(_t)
            .WHERE(_t.code >= P(1));

        using (IDataReader result = await _conn.ExecuteReaderAsync(sql))
        {
            Assert.True(result.Read());
            Assert.True(result.Read());
            Assert.False(result.Read());
        }
    }
}
#endif
