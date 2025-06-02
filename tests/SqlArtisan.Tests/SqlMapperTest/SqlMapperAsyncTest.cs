#if SQL_MAPPER_TEST
using System.Data;
using SqlArtisan.Dapper;
using static Dapper.SqlMapper;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

[Collection("SequentialTests")]
public class SqlMapperAsyncTest : SqlMapperTestBase
{
    private readonly TestTable _t = new();

    [Fact]
    public async Task ExecuteScalarAsync_NoTypeParameter_ReturnsObjectValue()
    {
        ISqlBuilder sql =
            Select(_t.Code)
            .From(_t)
            .Where(_t.Code == 1);

        object? result = await _conn.ExecuteScalarAsync(sql);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteScalarAsync_IntTypeParameter_ReturnsIntValue()
    {
        ISqlBuilder sql =
            Select(_t.Code)
            .From(_t)
            .Where(_t.Code == 1);

        int result = await _conn.ExecuteScalarAsync<int>(sql);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteScalarAsync_StringTypeParameter_ReturnsStringValue()
    {
        ISqlBuilder sql =
            Select(_t.Name)
            .From(_t)
            .Where(_t.Code == 1);

        string? result = await _conn.ExecuteScalarAsync<string>(sql);

        Assert.Equal("Test1", result);
    }

    [Fact]
    public async Task ExecuteScalarAsync_DateTimeTypeParameter_ReturnsDateTimeValue()
    {
        ISqlBuilder sql =
            Select(_t.CreatedAt)
            .From(_t)
            .Where(_t.Code == 1);

        DateTime result = await _conn.ExecuteScalarAsync<DateTime>(sql);

        Assert.Equal(new DateTime(2025, 4, 1), result);
    }

    [Fact]
    public async Task QuerySingleAsync_TestTableDtoType_ReturnsObjectValue()
    {
        ISqlBuilder sql =
            Select(
                _t.Code,
                _t.Name,
                _t.CreatedAt)
            .From(_t)
            .Where(_t.Code == 1);

        object result = await _conn.QuerySingleAsync(typeof(TestTableDto), sql);
        TestTableDto dto = ((TestTableDto)result);

        Assert.Equal(1, dto.Code);
        Assert.Equal("Test1", dto.Name);
        Assert.Equal(new DateTime(2025, 4, 1), dto.CreatedAt);
    }

    [Fact]
    public async Task QuerySingleAsync_NoType_ReturnsDynamicValue()
    {
        ISqlBuilder sql =
            Select(
                _t.Code,
                _t.Name,
                _t.CreatedAt)
            .From(_t)
            .Where(_t.Code == 1);

        dynamic result = await _conn.QuerySingleAsync(sql);
        IDictionary<string, object> row = (IDictionary<string, object>)result;

        Assert.Equal(1, row["CODE"]);
        Assert.Equal("Test1", row["NAME"]);
        Assert.Equal(new DateTime(2025, 4, 1), row["CREATED_AT"]);
    }

    [Fact]
    public async Task QuerySingleAsync_TestTableDtoType_ReturnsTestTableDtoValue()
    {
        ISqlBuilder sql =
            Select(
                _t.Code,
                _t.Name,
                _t.CreatedAt)
            .From(_t)
            .Where(_t.Code == 1);

        TestTableDto result = await _conn.QuerySingleAsync<TestTableDto>(sql);

        Assert.Equal(1, result.Code);
        Assert.Equal("Test1", result.Name);
        Assert.Equal(new DateTime(2025, 4, 1), result.CreatedAt);
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_TestTableDtoType_ReturnsObjectValue()
    {
        ISqlBuilder sql =
            Select(
                _t.Code,
                _t.Name,
                _t.CreatedAt)
            .From(_t)
            .Where(_t.Code == 1);

        object? result = await _conn.QuerySingleOrDefaultAsync(typeof(TestTableDto), sql);
        TestTableDto? dto = result as TestTableDto;

        Assert.Equal(1, dto?.Code);
        Assert.Equal("Test1", dto?.Name);
        Assert.Equal(new DateTime(2025, 4, 1), dto?.CreatedAt);
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_NoMatch_ReturnsNull()
    {
        ISqlBuilder sql =
            Select(
                _t.Code,
                _t.Name,
                _t.CreatedAt)
            .From(_t)
            .Where(_t.Code == 999);

        object? result = await _conn.QuerySingleOrDefaultAsync(typeof(TestTableDto), sql);

        Assert.Null(result);
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_NoType_ReturnsDynamicValue()
    {
        ISqlBuilder sql =
            Select(
                _t.Code,
                _t.Name,
                _t.CreatedAt)
            .From(_t)
            .Where(_t.Code == 1);

        dynamic? result = await _conn.QuerySingleOrDefaultAsync(sql);
        IDictionary<string, object>? row = result as IDictionary<string, object>;

        Assert.Equal(1, row?["CODE"]);
        Assert.Equal("Test1", row?["NAME"]);
        Assert.Equal(new DateTime(2025, 4, 1), row?["CREATED_AT"]);
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_NoTypeNoMatch_ReturnsNull()
    {
        ISqlBuilder sql =
            Select(
                _t.Code,
                _t.Name,
                _t.CreatedAt)
            .From(_t)
            .Where(_t.Code == 999);

        dynamic? result = await _conn.QuerySingleOrDefaultAsync(sql);
        IDictionary<string, object>? row = result as IDictionary<string, object>;

        Assert.Null(row);
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_TestTableDtoType_ReturnsTestTableDtoValue()
    {
        ISqlBuilder sql =
            Select(
                _t.Code,
                _t.Name,
                _t.CreatedAt)
            .From(_t)
            .Where(_t.Code == 1);

        TestTableDto? result = await _conn.QuerySingleOrDefaultAsync<TestTableDto>(sql);

        Assert.Equal(1, result?.Code);
        Assert.Equal("Test1", result?.Name);
        Assert.Equal(new DateTime(2025, 4, 1), result?.CreatedAt);
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_TestTableDtoTypeNoMatch_ReturnsNull()
    {
        ISqlBuilder sql =
            Select(
                _t.Code,
                _t.Name,
                _t.CreatedAt)
            .From(_t)
            .Where(_t.Code == 999);

        TestTableDto? result = await _conn.QuerySingleOrDefaultAsync<TestTableDto>(sql);

        Assert.Null(result);
    }

    [Fact]
    public async Task QueryFirstAsync_TestTableDtoType_ReturnsObjectValue()
    {
        ISqlBuilder sql =
            Select(
                _t.Code,
                _t.Name,
                _t.CreatedAt)
            .From(_t)
            .Where(_t.Code == 1);

        object result = await _conn.QueryFirstAsync(typeof(TestTableDto), sql);
        TestTableDto dto = ((TestTableDto)result);

        Assert.Equal(1, dto.Code);
        Assert.Equal("Test1", dto.Name);
        Assert.Equal(new DateTime(2025, 4, 1), dto.CreatedAt);
    }

    [Fact]
    public async Task QueryFirstAsync_NoType_ReturnsDynamicValue()
    {
        ISqlBuilder sql =
            Select(
                _t.Code,
                _t.Name,
                _t.CreatedAt)
            .From(_t)
            .Where(_t.Code == 1);

        dynamic result = await _conn.QueryFirstAsync(sql);
        IDictionary<string, object> row = (IDictionary<string, object>)result;

        Assert.Equal(1, row["CODE"]);
        Assert.Equal("Test1", row["NAME"]);
        Assert.Equal(new DateTime(2025, 4, 1), row["CREATED_AT"]);
    }

    [Fact]
    public async Task QueryFirstAsync_TestTableDtoType_ReturnsTestTableDtoValue()
    {
        ISqlBuilder sql =
            Select(
                _t.Code,
                _t.Name,
                _t.CreatedAt)
            .From(_t)
            .Where(_t.Code == 1);

        TestTableDto result = await _conn.QueryFirstAsync<TestTableDto>(sql);

        Assert.Equal(1, result.Code);
        Assert.Equal("Test1", result.Name);
        Assert.Equal(new DateTime(2025, 4, 1), result.CreatedAt);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_TestTableDtoType_ReturnsObjectValue()
    {
        ISqlBuilder sql =
            Select(
                _t.Code,
                _t.Name,
                _t.CreatedAt)
            .From(_t)
            .Where(_t.Code == 1);

        object? result = await _conn.QueryFirstOrDefaultAsync(typeof(TestTableDto), sql);
        TestTableDto? dto = result as TestTableDto;

        Assert.Equal(1, dto?.Code);
        Assert.Equal("Test1", dto?.Name);
        Assert.Equal(new DateTime(2025, 4, 1), dto?.CreatedAt);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_NoMatch_ReturnsNull()
    {
        ISqlBuilder sql =
            Select(
                _t.Code,
                _t.Name,
                _t.CreatedAt)
            .From(_t)
            .Where(_t.Code == 999);

        object? result = await _conn.QueryFirstOrDefaultAsync(typeof(TestTableDto), sql);

        Assert.Null(result);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_NoType_ReturnsDynamicValue()
    {
        ISqlBuilder sql =
            Select(
                _t.Code,
                _t.Name,
                _t.CreatedAt)
            .From(_t)
            .Where(_t.Code == 1);

        dynamic? result = await _conn.QueryFirstOrDefaultAsync(sql);
        IDictionary<string, object>? row = result as IDictionary<string, object>;

        Assert.Equal(1, row?["CODE"]);
        Assert.Equal("Test1", row?["NAME"]);
        Assert.Equal(new DateTime(2025, 4, 1), row?["CREATED_AT"]);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_NoTypeNoMatch_ReturnsNull()
    {
        ISqlBuilder sql =
            Select(
                _t.Code,
                _t.Name,
                _t.CreatedAt)
            .From(_t)
            .Where(_t.Code == 999);

        dynamic? result = await _conn.QueryFirstOrDefaultAsync(sql);
        IDictionary<string, object>? row = result as IDictionary<string, object>;

        Assert.Null(row);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_TestTableDtoType_ReturnsTestTableDtoValue()
    {
        ISqlBuilder sql =
            Select(
                _t.Code,
                _t.Name,
                _t.CreatedAt)
            .From(_t)
            .Where(_t.Code == 1);

        TestTableDto? result = await _conn.QueryFirstOrDefaultAsync<TestTableDto>(sql);

        Assert.Equal(1, result?.Code);
        Assert.Equal("Test1", result?.Name);
        Assert.Equal(new DateTime(2025, 4, 1), result?.CreatedAt);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_TestTableDtoTypeNoMatch_ReturnsNull()
    {
        ISqlBuilder sql =
            Select(
                _t.Code,
                _t.Name,
                _t.CreatedAt)
            .From(_t)
            .Where(_t.Code == 999);

        TestTableDto? result = await _conn.QueryFirstOrDefaultAsync<TestTableDto>(sql);

        Assert.Null(result);
    }

    [Fact]
    public async Task QueryAsync_TestTableDtoType_ReturnsObjectCollection()
    {
        ISqlBuilder sql =
            Select(
                _t.Code,
                _t.Name,
                _t.CreatedAt)
            .From(_t)
            .Where(_t.Code >= 1);

        IEnumerable<object> result = await _conn.QueryAsync(typeof(TestTableDto), sql);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task QueryAsync_NoType_ReturnsDynamicCollection()
    {
        ISqlBuilder sql =
            Select(
                _t.Code,
                _t.Name,
                _t.CreatedAt)
            .From(_t)
            .Where(_t.Code >= 1);

        IEnumerable<dynamic> result = await _conn.QueryAsync(sql);

        Assert.Equal(2, result.Count());
    }


    [Fact]
    public async Task QueryAsync_TestTableDtoType_ReturnsTestTableDtoCollection()
    {
        ISqlBuilder sql =
            Select(
                _t.Code,
                _t.Name,
                _t.CreatedAt)
            .From(_t)
            .Where(_t.Code >= 1);

        IEnumerable<TestTableDto> result = await _conn.QueryAsync<TestTableDto>(sql);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task QueryMultipleAsync_MatchesTwoRecords_ReturnsGridReader()
    {
        ISqlBuilder sql =
            Select(
                _t.Code,
                _t.Name,
                _t.CreatedAt)
            .From(_t)
            .Where(_t.Code >= 1);

        using (GridReader result = await _conn.QueryMultipleAsync(sql))
        {
            IEnumerable<dynamic> rows = result.Read();
            Assert.Equal(2, rows.Count());
        }
    }

    [Fact]
    public async Task ExecuteReaderAsync_MatchesTwoRecords_ReturnsIDataReader()
    {
        ISqlBuilder sql =
            Select(
                _t.Code,
                _t.Name,
                _t.CreatedAt)
            .From(_t)
            .Where(_t.Code >= 1);

        using (IDataReader result = await _conn.ExecuteReaderAsync(sql))
        {
            Assert.True(result.Read());
            Assert.True(result.Read());
            Assert.False(result.Read());
        }
    }
}
#endif
