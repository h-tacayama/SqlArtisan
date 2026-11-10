using System.Data;
using Microsoft.Data.Sqlite;
using SqlArtisan.Dapper;
using static SqlArtisan.Sql;
using DapperMapper = SqlArtisan.Dapper.SqlMapper;

namespace SqlArtisan.Tests;

// Dapper's handlers and SqlArtisan's converters are both process-wide, so these run alone
// and reset both (#810).
[Collection("NonParallelTests")]
public class TypeHandlerTests : IDisposable
{
    private readonly TestTable _t = new("t");

    public void Dispose() => DapperMapper.ResetTypeHandlers();

    [Fact]
    public void AddTypeHandler_ValueObject_BindsWhatSetValueSets()
    {
        DapperMapper.AddTypeHandler(new CustomerIdHandler());

        SqlStatement sql = Select(_t.Name).From(_t).Where(_t.Code == new CustomerId(2)).Build();

        Assert.Equal("SELECT \"t\".name FROM test_table \"t\" WHERE \"t\".code = :0", sql.Text);
        Assert.Equal(2L, sql.Parameters.Get<object>(":0"));
        Assert.True(global::Dapper.SqlMapper.HasTypeHandler(typeof(CustomerId)));
    }

    [Fact]
    public void AddTypeHandler_HandlerSettingDbTypeAndSize_CarriesBoth()
    {
        DapperMapper.AddTypeHandler(new EmailHandler());
        List<BindValue> binds = [];

        SqlStatement sql =
            Select(_t.Code).From(_t).Where(_t.Name == new Email("a@example.com")).Build();
        sql.Parameters.ForEach((_, bind) => binds.Add(bind));

        BindValue bind = Assert.Single(binds);
        Assert.Equal("a@example.com", bind.Value);
        Assert.Equal(DbType.AnsiString, bind.DbType);
        Assert.Equal(320, bind.Size);
    }

    [Fact]
    public void AddTypeHandler_ListedType_RegistersWithDapperOnly()
    {
        DapperMapper.AddTypeHandler(new UtcDateTimeHandler());
        DateTime at = new(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        Assert.Equal(at, Bind(at).Value);
        Assert.True(global::Dapper.SqlMapper.HasTypeHandler(typeof(DateTime)));
    }

    [Fact]
    public void AddTypeHandler_AbstractType_RegistersWithDapperOnly()
    {
        DapperMapper.AddTypeHandler(new MoneyHandler());

        ArgumentException ex = Assert.Throws<ArgumentException>(() => _t.Code == new Yen(5));

        Assert.Equal($"Invalid type for SqlExpression: {typeof(Yen)}", ex.Message);
        Assert.True(global::Dapper.SqlMapper.HasTypeHandler(typeof(Money)));
    }

    [Fact]
    public void AddTypeHandler_QueryThroughDapper_ReturnsTheRowAndReadsTheValueObject()
    {
        DapperMapper.AddTypeHandler(new CustomerIdHandler());
        using SqliteConnection cnn = OpenSeededConnection();
        CustomersTable c = new();

        List<CustomerId> ids = [.. cnn.Query<CustomerId>(
            Select(c.Id).From(c).Where(c.Id.In(new CustomerId[] { new(1), new(3) })))];
        string? name = cnn.QuerySingleOrDefault<string>(
            Select(c.Name).From(c).Where(c.Id == new CustomerId(2)));

        Assert.Equal([new CustomerId(1), new CustomerId(3)], ids);
        Assert.Equal("Bob", name);
    }

    [Fact]
    public void AddTypeHandler_HandlerSettingDbNull_InsertsNullAndRejectsTheComparison()
    {
        DapperMapper.AddTypeHandler(new OptionalIdHandler());
        using SqliteConnection cnn = OpenSeededConnection();
        CustomersTable c = new();

        cnn.Execute(InsertInto(c, c.Id, c.Name).Values(new OptionalId(null), "Dan"));
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => Select(c.Name).From(c).Where(c.Id == new OptionalId(null)).Build());

        Assert.Equal(
            ["Dan"], cnn.Query<string>(Select(c.Name).From(c).Where(c.Id.IsNull)));
        Assert.Equal(
            $"= never matches the NULL the value converter for {typeof(OptionalId)} returned; "
                + "test for NULL with .IsNull or .IsNotNull.",
            ex.Message);
    }

    [Fact]
    public void AddTypeHandler_HandlerSettingNoValue_ThrowsInvalidOperationException()
    {
        DapperMapper.AddTypeHandler(new SilentHandler());

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => InsertInto(_t, _t.Code).Values(new SilentId(1)));

        Assert.Equal(
            $"The type handler for {typeof(SilentId)} set no parameter Value; "
                + "set DBNull.Value for SQL NULL.",
            ex.Message);
    }

    // pgvector's VectorTypeHandler has this shape: the provider takes the type itself.
    [Fact]
    public void AddTypeHandler_HandlerPassingItsOwnType_ThrowsAndBindValueStillBinds()
    {
        DapperMapper.AddTypeHandler(new PassThroughHandler());

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => _t.Code == new ProviderNative(1));

        Assert.Equal(
            $"The value converter for {typeof(ProviderNative)} produced "
                + $"{typeof(ProviderNative)}; a converter, or a type handler's SetValue, must "
                + "produce a listed type, a byte[], null or a BindValue carrying one, and a "
                + "provider's own type binds at the call through new BindValue(value).",
            ex.Message);
        SqlStatement sql = Select(_t.Name)
            .From(_t)
            .Where(_t.Code == new BindValue(new ProviderNative(1)))
            .Build();
        Assert.Equal(new ProviderNative(1), sql.Parameters.Get<object>(":0"));
    }

    [Fact]
    public void ResetTypeHandlers_RegisteredType_ClearsDapperAndSqlArtisan()
    {
        DapperMapper.AddTypeHandler(new CustomerIdHandler());
        DapperMapper.ResetTypeHandlers();

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => _t.Code == new CustomerId(2));

        Assert.Equal($"Invalid type for SqlExpression: {typeof(CustomerId)}", ex.Message);
        Assert.False(global::Dapper.SqlMapper.HasTypeHandler(typeof(CustomerId)));
    }

    [Fact]
    public void AddTypeHandler_HandlerCastingToAProviderParameter_ThrowsInvalidCastException()
    {
        DapperMapper.AddTypeHandler(new SqliteOnlyHandler());

        Assert.Throws<InvalidCastException>(() => _t.Code == new OrderId(5));
    }

    [Fact]
    public void AddTypeHandler_NullHandler_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => DapperMapper.AddTypeHandler<CustomerId>(null!));

        Assert.Equal("handler", ex.ParamName);
    }

    private static SqliteConnection OpenSeededConnection()
    {
        SqliteConnection cnn = new("Data Source=:memory:");
        cnn.Open();
        using SqliteCommand command = cnn.CreateCommand();
        command.CommandText =
            "CREATE TABLE customers (id INTEGER, name TEXT);"
                + "INSERT INTO customers VALUES (1, 'Alice'), (2, 'Bob'), (3, 'Carol');";
        command.ExecuteNonQuery();
        return cnn;
    }

    public readonly record struct CustomerId(long Value);

    public readonly record struct OrderId(long Value);

    public readonly record struct OptionalId(long? Value);

    public readonly record struct SilentId(long Value);

    public readonly record struct ProviderNative(long Value);

    public sealed record Email(string Value);

    public abstract record Money(long Cents);

    public sealed record Yen(long Cents) : Money(Cents);

    private sealed class CustomersTable : DbTableBase
    {
        public CustomersTable() : base("customers", string.Empty)
        {
            Id = new DbColumn(this, "id");
            Name = new DbColumn(this, "name");
        }

        public DbColumn Id { get; }

        public DbColumn Name { get; }
    }

    private sealed class CustomerIdHandler : global::Dapper.SqlMapper.TypeHandler<CustomerId>
    {
        public override void SetValue(IDbDataParameter parameter, CustomerId value) =>
            parameter.Value = value.Value;

        public override CustomerId Parse(object value) => new(Convert.ToInt64(value));
    }

    private sealed class OptionalIdHandler : global::Dapper.SqlMapper.TypeHandler<OptionalId>
    {
        public override void SetValue(IDbDataParameter parameter, OptionalId value) =>
            parameter.Value = value.Value is long id ? id : DBNull.Value;

        public override OptionalId Parse(object value) => new(Convert.ToInt64(value));
    }

    private sealed class PassThroughHandler : global::Dapper.SqlMapper.TypeHandler<ProviderNative>
    {
        public override void SetValue(IDbDataParameter parameter, ProviderNative value) =>
            parameter.Value = value;

        public override ProviderNative Parse(object value) => (ProviderNative)value;
    }

    private sealed class SilentHandler : global::Dapper.SqlMapper.TypeHandler<SilentId>
    {
        public override void SetValue(IDbDataParameter parameter, SilentId value)
        {
        }

        public override SilentId Parse(object value) => new(Convert.ToInt64(value));
    }

    private sealed class EmailHandler : global::Dapper.SqlMapper.TypeHandler<Email>
    {
        public override void SetValue(IDbDataParameter parameter, Email? value)
        {
            parameter.DbType = DbType.AnsiString;
            parameter.Size = 320;
            parameter.Value = value!.Value;
        }

        public override Email Parse(object value) => new((string)value);
    }

    private sealed class MoneyHandler : global::Dapper.SqlMapper.TypeHandler<Money>
    {
        public override void SetValue(IDbDataParameter parameter, Money? value) =>
            parameter.Value = value!.Cents;

        public override Money Parse(object value) => new Yen(Convert.ToInt64(value));
    }

    private sealed class UtcDateTimeHandler : global::Dapper.SqlMapper.TypeHandler<DateTime>
    {
        public override void SetValue(IDbDataParameter parameter, DateTime value) =>
            parameter.Value = value;

        public override DateTime Parse(object value) =>
            DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc);
    }

    private sealed class SqliteOnlyHandler : global::Dapper.SqlMapper.TypeHandler<OrderId>
    {
        public override void SetValue(IDbDataParameter parameter, OrderId value)
        {
            ((SqliteParameter)parameter).SqliteType = SqliteType.Integer;
            parameter.Value = value.Value;
        }

        public override OrderId Parse(object value) => new(Convert.ToInt64(value));
    }
}
