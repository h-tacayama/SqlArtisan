using SqlArtisan.TableClassGen;

namespace SqlArtisan.TableClassGen.Tests;

/// <summary>
/// The catalog type name reduces to one coarse category here, so the analyzer
/// never needs a five-dialect type table. A name no engine table claims stays
/// unknown, which reads as silence rather than as a guess.
/// </summary>
public class ColumnCategoryTests
{
    [Theory]
    [InlineData(Dbms.MySql, "varchar", DbColumnType.Text)]
    [InlineData(Dbms.MySql, "longtext", DbColumnType.Text)]
    [InlineData(Dbms.MySql, "mediumint", DbColumnType.Numeric)]
    [InlineData(Dbms.MySql, "year", DbColumnType.Temporal)]
    [InlineData(Dbms.MySql, "longblob", DbColumnType.Binary)]
    [InlineData(Dbms.Oracle, "VARCHAR2", DbColumnType.Text)]
    [InlineData(Dbms.Oracle, "NUMBER", DbColumnType.Numeric)]
    [InlineData(Dbms.Oracle, "CLOB", DbColumnType.Text)]
    [InlineData(Dbms.Oracle, "RAW", DbColumnType.Binary)]
    [InlineData(Dbms.PostgreSql, "character varying", DbColumnType.Text)]
    [InlineData(Dbms.PostgreSql, "double precision", DbColumnType.Numeric)]
    [InlineData(Dbms.PostgreSql, "timestamp without time zone", DbColumnType.Temporal)]
    [InlineData(Dbms.PostgreSql, "bytea", DbColumnType.Binary)]
    [InlineData(Dbms.PostgreSql, "boolean", DbColumnType.Boolean)]
    [InlineData(Dbms.SqlServer, "nvarchar", DbColumnType.Text)]
    [InlineData(Dbms.SqlServer, "smallmoney", DbColumnType.Numeric)]
    [InlineData(Dbms.SqlServer, "datetime2", DbColumnType.Temporal)]
    [InlineData(Dbms.SqlServer, "image", DbColumnType.Binary)]
    public void Of_RecognizedTypeName_ReturnsCategory(Dbms dbms, string dataType, DbColumnType expected) =>
        Assert.Equal(expected, ColumnCategory.Of(dbms, dataType));

    // T-SQL's timestamp is a row version, not a time — the one name whose category
    // flips between engines rather than merely being absent from some.
    [Theory]
    [InlineData(Dbms.MySql, DbColumnType.Temporal)]
    [InlineData(Dbms.Oracle, DbColumnType.Temporal)]
    [InlineData(Dbms.PostgreSql, DbColumnType.Temporal)]
    [InlineData(Dbms.SqlServer, DbColumnType.Binary)]
    public void Of_Timestamp_ReturnsCategoryOfTheReadingEngine(Dbms dbms, DbColumnType expected) =>
        Assert.Equal(expected, ColumnCategory.Of(dbms, "timestamp"));

    [Fact]
    public void Of_SqlServer_Bit_ReturnsBoolean() =>
        Assert.Equal(DbColumnType.Boolean, ColumnCategory.Of(Dbms.SqlServer, "bit"));

    // Elsewhere a bit is a bit vector, and comparing one is not this mistake.
    [Theory]
    [InlineData(Dbms.MySql)]
    [InlineData(Dbms.PostgreSql)]
    public void Of_Bit_OutsideSqlServer_ReturnsUnknown(Dbms dbms) =>
        Assert.Null(ColumnCategory.Of(dbms, "bit"));

    [Theory]
    [InlineData("varchar(50)", DbColumnType.Text)]
    [InlineData("NUMBER(10,2)", DbColumnType.Numeric)]
    [InlineData("TIMESTAMP(6) WITH TIME ZONE", DbColumnType.Temporal)]
    public void Of_TypeNameCarryingPrecision_IgnoresIt(string dataType, DbColumnType expected) =>
        Assert.Equal(expected, ColumnCategory.Of(Dbms.Oracle, dataType));

    [Theory]
    [InlineData("INTEGER", DbColumnType.Numeric)]
    [InlineData("BIGINT", DbColumnType.Numeric)]
    [InlineData("VARCHAR(50)", DbColumnType.Text)]
    [InlineData("CLOB", DbColumnType.Text)]
    [InlineData("BLOB", DbColumnType.Binary)]
    [InlineData("", DbColumnType.Binary)]
    [InlineData("REAL", DbColumnType.Numeric)]
    [InlineData("DOUBLE", DbColumnType.Numeric)]
    public void Of_Sqlite_DeclaredType_ReturnsAffinityCategory(string dataType, DbColumnType expected) =>
        Assert.Equal(expected, ColumnCategory.Of(Dbms.Sqlite, dataType));

    // SQLite's last affinity rule sweeps every unmatched name into NUMERIC, which
    // would call a DATETIME column numeric while the value in it is conventionally
    // text. Left unknown instead, so the rule stays silent there.
    [Theory]
    [InlineData("DATETIME")]
    [InlineData("BOOLEAN")]
    [InlineData("DECIMAL(10,2)")]
    public void Of_Sqlite_TypeFallingToNumericAffinity_ReturnsUnknown(string dataType) =>
        Assert.Null(ColumnCategory.Of(Dbms.Sqlite, dataType));

    [Fact]
    public void Of_UnrecognizedTypeName_ReturnsUnknown() =>
        Assert.Null(ColumnCategory.Of(Dbms.PostgreSql, "geography"));

    // Which engine wrote the name decides what it means.
    [Fact]
    public void Of_UnknownDbms_ReturnsUnknown() =>
        Assert.Null(ColumnCategory.Of(Dbms.Unknown, "varchar"));
}
