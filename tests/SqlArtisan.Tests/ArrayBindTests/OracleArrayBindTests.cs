using Oracle.ManagedDataAccess.Client;
using SqlArtisan.ArrayBind;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class OracleArrayBindTests
{
    private static List<ISqlBuilder> TwoInsertStatements(ArrayBindTestTable t) =>
    [
        InsertInto(t, t.Id, t.Code, t.Qty, t.Price, t.Name, t.CreatedAt)
            .Values(1L, 10, (short)2, 9.99m, "a", new DateTime(2026, 1, 2, 3, 4, 5)),
        InsertInto(t, t.Id, t.Code, t.Qty, t.Price, t.Name, t.CreatedAt)
            .Values(2L, BindNull(), (short)3, 19.50m, BindNull(), new DateTime(2026, 2, 3, 4, 5, 6)),
    ];

    [Fact]
    public void ExecuteArrayBind_TwoRows_CorrectSql()
    {
        using OracleConnection connection = new();
        ArrayBindTestTable t = new();

        using OracleCommand command = OracleArrayBindCommandFactory.Create(
            connection, TwoInsertStatements(t), transaction: null);

        Assert.Equal(
            "INSERT INTO bulk_test (id, code, qty, price, name, created_at) "
                + "VALUES (:0, :1, :2, :3, :4, :5)",
            command.CommandText);
    }

    [Fact]
    public void ExecuteArrayBind_TwoRows_SetsArrayBindCount()
    {
        using OracleConnection connection = new();
        ArrayBindTestTable t = new();

        using OracleCommand command = OracleArrayBindCommandFactory.Create(
            connection, TwoInsertStatements(t), transaction: null);

        Assert.Equal(2, command.ArrayBindCount);
    }

    [Fact]
    public void ExecuteArrayBind_TwoRows_BindsPerColumnValueArrays()
    {
        using OracleConnection connection = new();
        ArrayBindTestTable t = new();

        using OracleCommand command = OracleArrayBindCommandFactory.Create(
            connection, TwoInsertStatements(t), transaction: null);

        Assert.Equal(6, command.Parameters.Count);

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
    public void ExecuteArrayBind_BindNull_BindsDbNullAtThatPosition()
    {
        using OracleConnection connection = new();
        ArrayBindTestTable t = new();

        using OracleCommand command = OracleArrayBindCommandFactory.Create(
            connection, TwoInsertStatements(t), transaction: null);

        Assert.Equal([10, DBNull.Value], (object[])command.Parameters[1].Value!);
        Assert.Equal(["a", DBNull.Value], (object[])command.Parameters[4].Value!);
    }

    [Fact]
    public void ExecuteArrayBind_AllNullColumnWithDbTypeHint_InfersOracleDbType()
    {
        using OracleConnection connection = new();
        ArrayBindTestTable t = new();
        List<ISqlBuilder> statements =
        [
            InsertInto(t, t.Id, t.Code).Values(1L, BindNull(System.Data.DbType.Int32)),
            InsertInto(t, t.Id, t.Code).Values(2L, BindNull(System.Data.DbType.Int32)),
        ];

        using OracleCommand command = OracleArrayBindCommandFactory.Create(
            connection, statements, transaction: null);

        Assert.Equal(OracleDbType.Int32, command.Parameters[1].OracleDbType);
        Assert.Equal([DBNull.Value, DBNull.Value], (object[])command.Parameters[1].Value!);
    }

    [Fact]
    public void ExecuteArrayBind_AllNullColumnWithoutDbTypeHint_ThrowsArgumentException()
    {
        using OracleConnection connection = new();
        ArrayBindTestTable t = new();
        List<ISqlBuilder> statements =
        [
            InsertInto(t, t.Id, t.Code).Values(1L, BindNull()),
            InsertInto(t, t.Id, t.Code).Values(2L, BindNull()),
        ];

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            OracleArrayBindCommandFactory.Create(connection, statements, transaction: null));

        Assert.Equal(
            "ExecuteArrayBind cannot infer an OracleDbType for parameter :1; every bound value is "
                + "null. Use Sql.BindNull(dbType) on at least one row to state the type explicitly.",
            ex.Message);
    }

    [Fact]
    public void ExecuteArrayBind_ConflictingDbTypeHints_ThrowsArgumentException()
    {
        using OracleConnection connection = new();
        ArrayBindTestTable t = new();
        List<ISqlBuilder> statements =
        [
            InsertInto(t, t.Id, t.Code).Values(1L, BindNull(System.Data.DbType.Int32)),
            InsertInto(t, t.Id, t.Code).Values(2L, BindNull(System.Data.DbType.String)),
        ];

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            OracleArrayBindCommandFactory.Create(connection, statements, transaction: null));

        Assert.Equal(
            "ExecuteArrayBind requires every row's Sql.BindNull(dbType) hint at parameter :1 to agree; "
                + "found both DbType.Int32 and DbType.String.",
            ex.Message);
    }

    [Fact]
    public void ExecuteArrayBind_DbTypeHintConflictsWithRealValue_ThrowsArgumentException()
    {
        using OracleConnection connection = new();
        ArrayBindTestTable t = new();
        List<ISqlBuilder> statements =
        [
            InsertInto(t, t.Id, t.CreatedAt).Values(1L, new DateTime(2026, 1, 1)),
            InsertInto(t, t.Id, t.CreatedAt).Values(2L, BindNull(System.Data.DbType.Int32)),
        ];

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            OracleArrayBindCommandFactory.Create(connection, statements, transaction: null));

        Assert.Equal(
            "ExecuteArrayBind cannot bind parameter :1 as OracleDbType.Int32 from Sql.BindNull(DbType.Int32); "
                + "another row binds a DateTime value there, which maps to OracleDbType.TimeStamp instead.",
            ex.Message);
    }

    [Fact]
    public void ExecuteArrayBind_MixedValueTypesAtOnePosition_ThrowsArgumentException()
    {
        using OracleConnection connection = new();
        ArrayBindTestTable t = new();
        List<ISqlBuilder> statements =
        [
            InsertInto(t, t.Id, t.Price).Values(1L, 9),
            InsertInto(t, t.Id, t.Price).Values(2L, 19.50m),
        ];

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            OracleArrayBindCommandFactory.Create(connection, statements, transaction: null));

        Assert.Equal(
            "ExecuteArrayBind requires every bound value at parameter :1 to map to the same OracleDbType; "
                + "a Int32 value maps to OracleDbType.Int32, but a Decimal value maps to OracleDbType.Decimal.",
            ex.Message);
    }

    [Fact]
    public void ExecuteArrayBind_MismatchedShape_ThrowsArgumentException()
    {
        using OracleConnection connection = new();
        ArrayBindTestTable t = new();
        List<ISqlBuilder> statements =
        [
            InsertInto(t, t.Id).Values(1L),
            InsertInto(t, t.Id, t.Code).Values(2L, 20),
        ];

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            OracleArrayBindCommandFactory.Create(connection, statements, transaction: null));

        Assert.Equal(
            "ExecuteArrayBind requires every statement to build identical SQL text; "
                + "statement at index 1 differs from index 0.",
            ex.Message);
    }

    [Fact]
    public void ExecuteArrayBind_Command_UsesGivenConnection()
    {
        using OracleConnection connection = new();
        ArrayBindTestTable t = new();

        using OracleCommand command = OracleArrayBindCommandFactory.Create(
            connection, TwoInsertStatements(t), transaction: null);

        Assert.Same(connection, command.Connection);
        Assert.Null(command.Transaction);
    }

    [Fact]
    public void ExecuteArrayBind_UpdateStatements_TransposesSetAndWhereValues()
    {
        using OracleConnection connection = new();
        ArrayBindTestTable t = new();
        List<ISqlBuilder> statements =
        [
            Update(t).Set(t.Price == 9.99m).Where(t.Id == 1L),
            Update(t).Set(t.Price == 19.50m).Where(t.Id == 2L),
        ];

        using OracleCommand command = OracleArrayBindCommandFactory.Create(
            connection, statements, transaction: null);

        Assert.Equal("UPDATE bulk_test SET price = :0 WHERE id = :1", command.CommandText);
        Assert.Equal(2, command.ArrayBindCount);
        Assert.Equal([9.99m, 19.50m], (object[])command.Parameters[0].Value!);
        Assert.Equal([1L, 2L], (object[])command.Parameters[1].Value!);
    }

    [Fact]
    public void ExecuteArrayBind_EmptyStatements_ThrowsArgumentException()
    {
        using OracleConnection connection = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            connection.ExecuteArrayBind(System.Array.Empty<ISqlBuilder>()));

        Assert.Equal("ExecuteArrayBind requires at least one statement.", ex.Message);
    }

    [Fact]
    public async Task ExecuteArrayBindAsync_EmptyStatements_ThrowsArgumentException()
    {
        using OracleConnection connection = new();

        ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            connection.ExecuteArrayBindAsync(System.Array.Empty<ISqlBuilder>()));

        Assert.Equal("ExecuteArrayBind requires at least one statement.", ex.Message);
    }
}
