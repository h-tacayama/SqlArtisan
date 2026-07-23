using Oracle.ManagedDataAccess.Client;
using SqlArtisan.BulkCopy;

namespace SqlArtisan.Tests;

public class OracleBulkInsertTests
{
    private static readonly BulkTestRow[] TwoRows =
    [
        new()
        {
            Id = 1,
            Code = 10,
            Qty = 2,
            Price = 9.99m,
            Name = "a",
            CreatedAt = new DateTime(2026, 1, 2, 3, 4, 5),
        },
        new()
        {
            Id = 2,
            Code = null,
            Qty = 3,
            Price = 19.50m,
            Name = null,
            CreatedAt = new DateTime(2026, 2, 3, 4, 5, 6),
        },
    ];

    private static OracleCommand CreateTwoRowCommand(OracleConnection connection) =>
        OracleBulkInsertCommandFactory.Create(
            connection, new BulkTestTable(), TwoRows, transaction: null);

    [Fact]
    public void BulkInsert_TwoRows_CorrectSql()
    {
        using OracleConnection connection = new();
        using OracleCommand command = CreateTwoRowCommand(connection);

        Assert.Equal(
            "INSERT INTO bulk_test (id, code, qty, price, name, created_at) "
                + "VALUES (:0, :1, :2, :3, :4, :5)",
            command.CommandText);
    }

    [Fact]
    public void BulkInsert_TwoRows_SetsArrayBindCount()
    {
        using OracleConnection connection = new();
        using OracleCommand command = CreateTwoRowCommand(connection);

        Assert.Equal(2, command.ArrayBindCount);
    }

    [Fact]
    public void BulkInsert_TwoRows_BindsPerColumnValueArrays()
    {
        using OracleConnection connection = new();
        using OracleCommand command = CreateTwoRowCommand(connection);

        Assert.Equal(6, command.Parameters.Count);

        string[] expectedNames = ["0", "1", "2", "3", "4", "5"];
        OracleDbType[] expectedTypes =
        [
            OracleDbType.Int64,
            OracleDbType.Int32,
            OracleDbType.Int16,
            OracleDbType.Decimal,
            OracleDbType.Varchar2,
            OracleDbType.TimeStamp,
        ];

        for (int i = 0; i < 6; i++)
        {
            Assert.Equal(expectedNames[i], command.Parameters[i].ParameterName);
            Assert.Equal(expectedTypes[i], command.Parameters[i].OracleDbType);
        }

        Assert.Equal([1L, 2L], (object[])command.Parameters[0].Value!);
        Assert.Equal([(short)2, (short)3], (object[])command.Parameters[2].Value!);
        Assert.Equal([9.99m, 19.50m], (object[])command.Parameters[3].Value!);
        Assert.Equal(
            [
                new DateTime(2026, 1, 2, 3, 4, 5),
                new DateTime(2026, 2, 3, 4, 5, 6),
            ],
            (object[])command.Parameters[5].Value!);
    }

    [Fact]
    public void BulkInsert_NullValues_BindsDbNull()
    {
        using OracleConnection connection = new();
        using OracleCommand command = CreateTwoRowCommand(connection);

        Assert.Equal([10, DBNull.Value], (object[])command.Parameters[1].Value!);
        Assert.Equal(["a", DBNull.Value], (object[])command.Parameters[4].Value!);
    }

    [Fact]
    public void BulkInsert_NullableProperty_UnwrapsOracleDbType()
    {
        using OracleConnection connection = new();
        using OracleCommand command = CreateTwoRowCommand(connection);

        Assert.Equal(OracleDbType.Int32, command.Parameters[1].OracleDbType);
        Assert.Equal(OracleDbType.Varchar2, command.Parameters[4].OracleDbType);
    }

    [Fact]
    public void BulkInsert_Command_UsesGivenConnection()
    {
        using OracleConnection connection = new();

        using OracleCommand command = OracleBulkInsertCommandFactory.Create(
            connection, new BulkTestTable(), TwoRows, transaction: null);

        Assert.Same(connection, command.Connection);
        Assert.Null(command.Transaction);
    }

    [Fact]
    public void BulkInsert_EmptyRows_ThrowsArgumentException()
    {
        using OracleConnection connection = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            connection.BulkInsert(new BulkTestTable(), Array.Empty<BulkTestRow>()));

        Assert.Equal("BulkInsert requires at least one row.", ex.Message);
    }

    [Fact]
    public async Task BulkInsertAsync_EmptyRows_ThrowsArgumentException()
    {
        using OracleConnection connection = new();

        ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            connection.BulkInsertAsync(new BulkTestTable(), Array.Empty<BulkTestRow>()));

        Assert.Equal("BulkInsert requires at least one row.", ex.Message);
    }

    [Fact]
    public void BulkInsert_NoColumnProperties_ThrowsArgumentException()
    {
        using OracleConnection connection = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            connection.BulkInsert(new EmptyBulkTestTable(), TwoRows));

        Assert.Equal(
            "BulkInsert requires the table class to expose at least one public DbColumn property.",
            ex.Message);
    }

    [Fact]
    public void BulkInsert_MissingRowProperty_ThrowsArgumentException()
    {
        using OracleConnection connection = new();
        BulkTestRowMissingProperty[] rows = [new() { Id = 1 }];

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            connection.BulkInsert(new BulkTestTable(), rows));

        Assert.Equal(
            "BulkInsert requires the row type 'BulkTestRowMissingProperty' to have a public "
                + "property 'CreatedAt' matching table class 'BulkTestTable'.",
            ex.Message);
    }

    [Fact]
    public void BulkInsert_UnsupportedPropertyType_ThrowsArgumentException()
    {
        using OracleConnection connection = new();
        BulkTestRowUnsupportedType[] rows = [new() { Id = 1 }];

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            connection.BulkInsert(new BulkTestTable(), rows));

        Assert.Equal(
            "BulkInsert cannot map property 'Qty' of type Boolean to an OracleDbType; "
                + "supported types are int, long, short, decimal, string, and DateTime, "
                + "plus their nullable forms.",
            ex.Message);
    }
}
