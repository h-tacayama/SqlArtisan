using System.Data;
using Oracle.ManagedDataAccess.Client;
using SqlArtisan.ArrayBind.Internal;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

// The registry is process-wide, so these run alone and leave it empty (#810).
[Collection("NonParallelTests")]
public class ValueConverterTests : IDisposable
{
    private static readonly CustomerId NoCustomer = new(0);

    private static readonly string[] Tags = ["a", "b"];

    private static readonly string AmbiguousList =
        $"A {typeof(string[])} passed as the whole argument list could be the list or one "
            + "value, since a value converter is registered for it; pass (object)value to bind "
            + "it as one value, or [.. value] to pass its elements.";

    private readonly TestTable _t = new("t");

    public void Dispose() => SqlArtisanConfig.ResetValueConverters();

    [Fact]
    public void Comparison_RegisteredType_BindsTheConvertedValue()
    {
        SqlArtisanConfig.AddValueConverter<CustomerId>(id => id.Value);

        SqlStatement sql = Select(_t.Name).From(_t).Where(_t.Code == new CustomerId(2)).Build();

        Assert.Equal("SELECT \"t\".name FROM test_table \"t\" WHERE \"t\".code = :0", sql.Text);
        Assert.Equal(2L, sql.Parameters.Get<object>(":0"));
    }

    [Fact]
    public void In_RegisteredTypeArray_BindsEachConvertedValue()
    {
        SqlArtisanConfig.AddValueConverter<CustomerId>(id => id.Value);
        CustomerId[] ids = [new(1), new(3)];

        SqlStatement sql = Select(_t.Name).From(_t).Where(_t.Code.In(ids)).Build();

        Assert.Equal(
            "SELECT \"t\".name FROM test_table \"t\" WHERE \"t\".code IN (:0, :1)", sql.Text);
        Assert.Equal(1L, sql.Parameters.Get<object>(":0"));
        Assert.Equal(3L, sql.Parameters.Get<object>(":1"));
    }

    [Fact]
    public void InsertValues_RegisteredType_BindsTheConvertedValue()
    {
        SqlArtisanConfig.AddValueConverter<CustomerId>(id => id.Value);

        TestTable t = new();

        SqlStatement sql = InsertInto(t, t.Code, t.Name).Values(new CustomerId(9), "z").Build();

        Assert.Equal("INSERT INTO test_table (code, name) VALUES (:0, :1)", sql.Text);
        Assert.Equal(9L, sql.Parameters.Get<object>(":0"));
        Assert.Equal("z", sql.Parameters.Get<object>(":1"));
    }

    [Fact]
    public void Bind_RegisteredType_BindsTheConvertedValue()
    {
        SqlArtisanConfig.AddValueConverter<CustomerId>(id => id.Value);

        Assert.Equal(2L, Bind(new CustomerId(2)).Value);
    }

    [Fact]
    public void BindValue_RegisteredType_KeepsTheValue()
    {
        SqlArtisanConfig.AddValueConverter<CustomerId>(id => id.Value);

        Assert.Equal(new CustomerId(2), new BindValue(new CustomerId(2)).Value);
    }

    [Fact]
    public void Comparison_ConverterReturningBindValue_KeepsItsDbType()
    {
        SqlArtisanConfig.AddValueConverter<Email>(
            email => new BindValue(email.Value, DbType.AnsiString, size: 320));
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
    public void AddValueConverter_NullableType_ConvertsTheValue()
    {
        SqlArtisanConfig.AddValueConverter<CustomerId?>(id => id!.Value.Value);
        CustomerId? id = new CustomerId(2);

        SqlStatement sql = Select(_t.Name).From(_t).Where(_t.Code == id).Build();

        Assert.Equal(2L, sql.Parameters.Get<object>(":0"));
    }

    [Fact]
    public void Comparison_ConverterReturningByteArray_BindsTheBytes()
    {
        byte[] bytes = [1, 2, 3];
        SqlArtisanConfig.AddValueConverter<Hash>(hash => hash.Bytes);

        SqlStatement sql = Select(_t.Code).From(_t).Where(_t.Name == new Hash(bytes)).Build();

        Assert.Same(bytes, sql.Parameters.Get<object>(":0"));
    }

    [Fact]
    public void AddValueConverter_ValuePassedBeforeReregistration_KeepsTheFirstConversion()
    {
        SqlArtisanConfig.AddValueConverter<CustomerId>(id => id.Value);
        SqlCondition held = _t.Code == new CustomerId(2);
        SqlArtisanConfig.AddValueConverter<CustomerId>(id => id.Value * 100);

        SqlStatement sql = Select(_t.Name).From(_t).Where(held).Build();

        Assert.Equal(2L, sql.Parameters.Get<object>(":0"));
    }

    [Fact]
    public void AddValueConverter_SecondRegistration_ReplacesTheFirst()
    {
        SqlArtisanConfig.AddValueConverter<CustomerId>(id => id.Value);
        SqlArtisanConfig.AddValueConverter<CustomerId>(id => id.Value * 10);

        Assert.Equal(20L, Bind(new CustomerId(2)).Value);
    }

    [Fact]
    public void ExecuteArrayBind_RegisteredType_BindsTheConvertedValue()
    {
        SqlArtisanConfig.AddValueConverter<CustomerId>(id => id.Value);
        using OracleConnection connection = new();
        ArrayBindTestTable t = new();
        ISqlBuilder[] rows =
        [
            InsertInto(t, t.Id).Values(new CustomerId(1)),
            InsertInto(t, t.Id).Values(new CustomerId(2)),
        ];

        using OracleCommand command =
            OracleArrayBindCommandFactory.Create(connection, rows, transaction: null);

        Assert.Equal(OracleDbType.Int64, command.Parameters[0].OracleDbType);
        Assert.Equal([1L, 2L], (object[])command.Parameters[0].Value!);
    }

    [Fact]
    public void Comparison_UnregisteredType_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => _t.Code == new CustomerId(2));

        Assert.Equal($"Invalid type for SqlExpression: {typeof(CustomerId)}", ex.Message);
    }

    [Fact]
    public void ResetValueConverters_RegisteredType_ThrowsArgumentException()
    {
        SqlArtisanConfig.AddValueConverter<CustomerId>(id => id.Value);
        SqlArtisanConfig.ResetValueConverters();

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => _t.Code == new CustomerId(2));

        Assert.Equal($"Invalid type for SqlExpression: {typeof(CustomerId)}", ex.Message);
    }

    [Fact]
    public void OrderBy_RegisteredType_ThrowsArgumentException()
    {
        SqlArtisanConfig.AddValueConverter<CustomerId>(id => id.Value);

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => Select(_t.Name).From(_t).OrderBy(new CustomerId(2)));

        Assert.Equal($"Invalid type for OrderByItem: {typeof(CustomerId)}", ex.Message);
    }

    [Fact]
    public void BindArray_RegisteredElementType_ThrowsArgumentException()
    {
        SqlArtisanConfig.AddValueConverter<CustomerId>(id => id.Value);

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => BindArray(new CustomerId[] { new(1) }));

        Assert.Equal(
            $"BindArray does not apply the value converter for {typeof(CustomerId)}; "
                + "pass an array of the values it wraps.",
            ex.Message);
    }

    [Fact]
    public void AddValueConverter_ListedType_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => SqlArtisanConfig.AddValueConverter<long>(value => value));

        Assert.Equal("System.Int64 binds without a value converter.", ex.Message);
    }

    [Fact]
    public void AddValueConverter_Interface_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => SqlArtisanConfig.AddValueConverter<IEntityId>(id => id.Raw));

        Assert.Equal(
            $"A value converter is found by the value's own type, so {typeof(IEntityId)} "
                + "must be a concrete type.",
            ex.Message);
    }

    [Fact]
    public void AddValueConverter_NullConverter_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => SqlArtisanConfig.AddValueConverter<CustomerId>(null!));

        Assert.Equal("convert", ex.ParamName);
    }

    [Fact]
    public void InsertValues_ConverterReturningNull_BindsSqlNull()
    {
        RegisterNoneAsNull();
        TestTable t = new();

        SqlStatement sql = InsertInto(t, t.Code, t.Name).Values(NoCustomer, "z").Build();

        Assert.Equal("INSERT INTO test_table (code, name) VALUES (:0, :1)", sql.Text);
        Assert.Equal(DBNull.Value, sql.Parameters.Get<object>(":0"));
    }

    [Fact]
    public void UpdateSet_ConverterReturningNull_BindsSqlNull()
    {
        RegisterNoneAsNull();
        TestTable t = new();

        SqlStatement sql = Update(t).Set(t.Code == NoCustomer).Where(t.Name == "z").Build();

        Assert.Equal("UPDATE test_table SET code = :0 WHERE name = :1", sql.Text);
        Assert.Equal(DBNull.Value, sql.Parameters.Get<object>(":0"));
    }

    // The joined arm renders SET through the qualified form, not through the condition.
    [Fact]
    public void UpdateSet_MySql_JoinedForm_ConverterReturningNull_BindsSqlNull()
    {
        RegisterNoneAsNull();
        TestTable t = new("t");
        TestTable s = new("s");

        SqlStatement sql =
            Update(t)
            .InnerJoin(s).On(t.Code == s.Code)
            .Set(t.Code == NoCustomer)
            .Build(Dbms.MySql);

        Assert.Equal(
            "UPDATE test_table AS `t` INNER JOIN test_table `s` ON `t`.code = `s`.code "
                + "SET `t`.code = ?0",
            sql.Text);
        Assert.Equal(DBNull.Value, sql.Parameters.Get<object>("?0"));
    }

    [Fact]
    public void InsertValues_ConverterReturningBindNullOfAType_KeepsTheDbType()
    {
        SqlArtisanConfig.AddValueConverter<CustomerId>(_ => BindNull(DbType.Int64));
        TestTable t = new();
        List<BindValue> binds = [];

        InsertInto(t, t.Code).Values(new CustomerId(2)).Build()
            .Parameters.ForEach((_, bind) => binds.Add(bind));

        BindValue bind = Assert.Single(binds);
        Assert.Equal(DBNull.Value, bind.Value);
        Assert.Equal(DbType.Int64, bind.DbType);
    }

    [Fact]
    public void Select_ConverterReturningNull_BindsSqlNull()
    {
        RegisterNoneAsNull();

        SqlStatement sql = Select(NoCustomer).From(_t).Build();

        Assert.Equal("SELECT :0 FROM test_table \"t\"", sql.Text);
        Assert.Equal(DBNull.Value, sql.Parameters.Get<object>(":0"));
    }

    [Fact]
    public void Where_ConverterReturningNull_ThrowsArgumentException()
    {
        RegisterNoneAsNull();

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => Select(_t.Name).From(_t).Where(_t.Code == NoCustomer).Build());

        Assert.Equal(NeverMatches("="), ex.Message);
    }

    [Fact]
    public void InReceiver_ConverterReturningNull_ThrowsArgumentException()
    {
        RegisterNoneAsNull();

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => Bind(NoCustomer).In(_t.Code, _t.Name));

        Assert.Equal(NeverMatches("IN"), ex.Message);
    }

    [Fact]
    public void Against_ConverterReturningNull_ThrowsArgumentException()
    {
        RegisterNoneAsNull();

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => Match(_t.Name).Against(NoCustomer));

        Assert.Equal(
            "AGAINST matches no row for the NULL the value converter for "
                + $"{typeof(CustomerId)} returned; pass the text, or leave the predicate out.",
            ex.Message);
    }

    [Fact]
    public void Contains_ConverterReturningNull_ThrowsArgumentException()
    {
        RegisterNoneAsNull();

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => Contains(_t.Name, NoCustomer));

        Assert.Equal(CannotTake("CONTAINS"), ex.Message);
    }

    [Fact]
    public void Any_ConverterReturningNull_ThrowsArgumentException()
    {
        RegisterNoneAsNull();

        ArgumentException ex = Assert.Throws<ArgumentException>(() => Any(Bind(NoCustomer)));

        Assert.Equal(CannotTake("ANY"), ex.Message);
    }

    [Fact]
    public void InsertValues_RegisteredArrayType_BindsTheConvertedValue()
    {
        SqlArtisanConfig.AddValueConverter<string[]>(tags => string.Join(",", tags));
        TestTable t = new();

        SqlStatement sql = InsertInto(t, t.Code, t.Name).Values(1, new[] { "a", "b" }).Build();

        Assert.Equal("INSERT INTO test_table (code, name) VALUES (:0, :1)", sql.Text);
        Assert.Equal("a,b", sql.Parameters.Get<object>(":1"));
    }

    [Fact]
    public void MergeThenInsert_RegisteredArrayType_BindsTheConvertedValue()
    {
        SqlArtisanConfig.AddValueConverter<string[]>(tags => string.Join(",", tags));
        TestTable t = new("t");
        TestTable s = new("s");

        SqlStatement sql = MergeInto(t)
            .Using(s)
            .On(t.Code == s.Code)
            .WhenNotMatched()
            .ThenInsert(t.Code, t.Name)
            .Values(s.Code, new[] { "a", "b" })
            .Build();

        Assert.Equal(
            "MERGE INTO test_table \"t\" USING test_table \"s\" ON (\"t\".code = \"s\".code) "
                + "WHEN NOT MATCHED THEN INSERT (code, name) VALUES (\"s\".code, :0)",
            sql.Text);
        Assert.Equal("a,b", sql.Parameters.Get<object>(":0"));
    }

    [Fact]
    public void FunctionArgument_ConverterReturningNull_BindsSqlNull()
    {
        RegisterNoneAsNull();

        SqlStatement sql = Select(Coalesce(_t.Code, NoCustomer)).From(_t).Build();

        Assert.Equal("SELECT COALESCE(\"t\".code, :0) FROM test_table \"t\"", sql.Text);
        Assert.Equal(DBNull.Value, sql.Parameters.Get<object>(":0"));
    }

    [Fact]
    public void SimpleCaseWhen_ConverterReturningNull_ThrowsArgumentException()
    {
        RegisterNoneAsNull();

        ArgumentException ex = Assert.Throws<ArgumentException>(() => When(NoCustomer));

        Assert.Equal(
            $"A simple CASE never matches the NULL the value converter for {typeof(CustomerId)} "
                + "returned; test for NULL with a searched When(expr.IsNull).",
            ex.Message);
    }

    [Fact]
    public void Comparison_ConverterReturningUnlistedType_ThrowsInvalidOperationException()
    {
        SqlArtisanConfig.AddValueConverter<CustomerId>(id => new Email(id.ToString()));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => _t.Code == new CustomerId(2));

        Assert.Equal(
            $"The value converter for {typeof(CustomerId)} produced {typeof(Email)}; a "
                + "converter, or a type handler's SetValue, must produce a type SqlArtisan binds "
                + "directly, a byte[], null or a BindValue carrying one, and a provider's own "
                + "type binds at the call through new BindValue(value).",
            ex.Message);
    }

    [Fact]
    public void Coalesce_RegisteredArrayTypeAsTheList_ThrowsArgumentException()
    {
        RegisterTags();

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => Coalesce(_t.Name, _t.Name, Tags));

        Assert.Equal(AmbiguousList, ex.Message);
    }

    [Fact]
    public void Coalesce_RegisteredArrayTypeCastToObject_BindsOneConvertedValue()
    {
        RegisterTags();

        SqlStatement sql = Select(Coalesce(_t.Name, _t.Name, (object)Tags)).From(_t).Build();

        Assert.Equal(
            "SELECT COALESCE(\"t\".name, \"t\".name, :0) FROM test_table \"t\"", sql.Text);
        Assert.Equal("a,b", sql.Parameters.Get<object>(":0"));
    }

    [Fact]
    public void Coalesce_RegisteredArraySpreadIntoTheList_BindsEachElement()
    {
        RegisterTags();

        SqlStatement sql = Select(Coalesce(_t.Name, _t.Name, [.. Tags])).From(_t).Build();

        Assert.Equal(
            "SELECT COALESCE(\"t\".name, \"t\".name, :0, :1) FROM test_table \"t\"",
            sql.Text);
        Assert.Equal("a", sql.Parameters.Get<object>(":0"));
        Assert.Equal("b", sql.Parameters.Get<object>(":1"));
    }

    [Fact]
    public void Select_RegisteredArrayTypeAsTheList_ThrowsArgumentException()
    {
        RegisterTags();

        ArgumentException ex = Assert.Throws<ArgumentException>(() => Select(Tags));

        Assert.Equal(AmbiguousList, ex.Message);
    }

    [Fact]
    public void Returning_RegisteredArrayTypeAsTheList_ThrowsArgumentException()
    {
        RegisterTags();
        TestTable t = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => InsertInto(t, t.Name).Values("x").Returning(Tags));

        Assert.Equal(AmbiguousList, ex.Message);
    }

    [Fact]
    public void Output_RegisteredArrayTypeAsTheList_ThrowsArgumentException()
    {
        RegisterTags();
        TestTable t = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => DeleteFrom(t).Output(Tags));

        Assert.Equal(AmbiguousList, ex.Message);
    }

    [Fact]
    public void InsertValues_RegisteredArrayTypeAsTheRow_ThrowsArgumentException()
    {
        RegisterTags();
        TestTable t = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => InsertInto(t, t.Name).Values(Tags));

        Assert.Equal(AmbiguousList, ex.Message);
    }

    [Fact]
    public void InsertValues_RegisteredArrayTypeCastToObject_BindsOneConvertedValue()
    {
        RegisterTags();
        TestTable t = new();

        SqlStatement sql = InsertInto(t, t.Name).Values((object)Tags).Build();

        Assert.Equal("INSERT INTO test_table (name) VALUES (:0)", sql.Text);
        Assert.Equal("a,b", sql.Parameters.Get<object>(":0"));
    }

    [Fact]
    public void InsertValues_RegisteredArrayTypeRows_ReadsEachAsARow()
    {
        RegisterTags();
        TestTable t = new();

        SqlStatement sql = InsertInto(t, t.Name, t.Code).Values(new[] { Tags, Tags }).Build();

        Assert.Equal("INSERT INTO test_table (name, code) VALUES (:0, :1), (:2, :3)", sql.Text);
        Assert.Equal("a", sql.Parameters.Get<object>(":0"));
        Assert.Equal("b", sql.Parameters.Get<object>(":3"));
    }

    [Fact]
    public void MergeValues_RegisteredArrayTypeAsTheRow_ThrowsArgumentException()
    {
        RegisterTags();
        TestTable t = new("t");
        TestTable s = new("s");

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => MergeInto(t)
                .Using(s)
                .On(t.Code == s.Code)
                .WhenNotMatched()
                .ThenInsert(t.Name)
                .Values(Tags));

        Assert.Equal(AmbiguousList, ex.Message);
    }

    [Fact]
    public void In_RegisteredArrayType_ReadsTheList()
    {
        RegisterTags();

        SqlStatement sql = Select(_t.Code).From(_t).Where(_t.Name.In(Tags)).Build();

        Assert.Equal(
            "SELECT \"t\".code FROM test_table \"t\" WHERE \"t\".name IN (:0, :1)", sql.Text);
        Assert.Equal("a", sql.Parameters.Get<object>(":0"));
    }

    [Fact]
    public void AddValueConverter_ObjectArray_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => SqlArtisanConfig.AddValueConverter<object[]>(values => values.Length));

        Assert.Equal(
            "A value converter cannot take System.Object[]: its value cannot be told from a "
                + "list of values.",
            ex.Message);
    }

    private static void RegisterNoneAsNull() =>
        SqlArtisanConfig.AddValueConverter<CustomerId>(id => id.Value == 0 ? null : id.Value);

    private static void RegisterTags() =>
        SqlArtisanConfig.AddValueConverter<string[]>(tags => string.Join(",", tags));

    private static string CannotTake(string construct) =>
        $"{construct} cannot take the NULL the value converter for {typeof(CustomerId)} "
            + "returned; pass a value, or leave the predicate out.";

    private static string NeverMatches(string construct) =>
        $"{construct} never matches the NULL the value converter for {typeof(CustomerId)} "
            + "returned; test for NULL with .IsNull or .IsNotNull.";

    public readonly record struct CustomerId(long Value);

    public sealed record Email(string Value);

    public sealed record Hash(byte[] Bytes);

    public interface IEntityId
    {
        long Raw { get; }
    }
}
