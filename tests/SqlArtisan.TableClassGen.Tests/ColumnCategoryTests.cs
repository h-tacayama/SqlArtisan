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
    [InlineData(Dbms.MySql, "varchar", DbTypeCategory.Text)]
    [InlineData(Dbms.MySql, "longtext", DbTypeCategory.Text)]
    [InlineData(Dbms.MySql, "mediumint", DbTypeCategory.Numeric)]
    [InlineData(Dbms.MySql, "year", DbTypeCategory.Temporal)]
    [InlineData(Dbms.MySql, "longblob", DbTypeCategory.Binary)]
    [InlineData(Dbms.Oracle, "VARCHAR2", DbTypeCategory.Text)]
    [InlineData(Dbms.Oracle, "NUMBER", DbTypeCategory.Numeric)]
    [InlineData(Dbms.Oracle, "CLOB", DbTypeCategory.Text)]
    [InlineData(Dbms.Oracle, "RAW", DbTypeCategory.Binary)]
    [InlineData(Dbms.PostgreSql, "character varying", DbTypeCategory.Text)]
    [InlineData(Dbms.PostgreSql, "double precision", DbTypeCategory.Numeric)]
    [InlineData(Dbms.PostgreSql, "timestamp without time zone", DbTypeCategory.Temporal)]
    [InlineData(Dbms.PostgreSql, "bytea", DbTypeCategory.Binary)]
    [InlineData(Dbms.PostgreSql, "boolean", DbTypeCategory.Boolean)]
    [InlineData(Dbms.SqlServer, "nvarchar", DbTypeCategory.Text)]
    [InlineData(Dbms.SqlServer, "smallmoney", DbTypeCategory.Numeric)]
    [InlineData(Dbms.SqlServer, "datetime2", DbTypeCategory.Temporal)]
    [InlineData(Dbms.SqlServer, "image", DbTypeCategory.Binary)]
    public void Of_RecognizedTypeName_ReturnsCategory(Dbms dbms, string dataType, DbTypeCategory expected) =>
        Assert.Equal(expected, ColumnCategory.Of(dbms, dataType));

    // T-SQL's timestamp is a row version, not a time — the one name whose category
    // flips between engines rather than merely being absent from some.
    [Theory]
    [InlineData(Dbms.MySql, DbTypeCategory.Temporal)]
    [InlineData(Dbms.Oracle, DbTypeCategory.Temporal)]
    [InlineData(Dbms.PostgreSql, DbTypeCategory.Temporal)]
    [InlineData(Dbms.SqlServer, DbTypeCategory.Binary)]
    public void Of_Timestamp_ReturnsCategoryOfTheReadingEngine(Dbms dbms, DbTypeCategory expected) =>
        Assert.Equal(expected, ColumnCategory.Of(dbms, "timestamp"));

    [Fact]
    public void Of_SqlServer_Bit_ReturnsBoolean() =>
        Assert.Equal(DbTypeCategory.Boolean, ColumnCategory.Of(Dbms.SqlServer, "bit"));

    // Elsewhere a bit is a bit vector, and comparing one is not this mistake.
    [Theory]
    [InlineData(Dbms.MySql)]
    [InlineData(Dbms.PostgreSql)]
    public void Of_Bit_OutsideSqlServer_ReturnsUnknown(Dbms dbms) =>
        Assert.Null(ColumnCategory.Of(dbms, "bit"));

    [Theory]
    [InlineData("varchar(50)", DbTypeCategory.Text)]
    [InlineData("NUMBER(10,2)", DbTypeCategory.Numeric)]
    [InlineData("TIMESTAMP(6) WITH TIME ZONE", DbTypeCategory.Temporal)]
    public void Of_TypeNameCarryingPrecision_IgnoresIt(string dataType, DbTypeCategory expected) =>
        Assert.Equal(expected, ColumnCategory.Of(Dbms.Oracle, dataType));

    // The precision sits mid-name in Oracle's interval range types, so stripping
    // it must not also strip the trailing field that names the type.
    [Theory]
    [InlineData("INTERVAL YEAR(2) TO MONTH")]
    [InlineData("INTERVAL DAY(2) TO SECOND(6)")]
    [InlineData("INTERVAL YEAR TO MONTH")]
    [InlineData("INTERVAL DAY TO SECOND")]
    public void Of_OracleIntervalRangeType_ReturnsTemporal(string dataType) =>
        Assert.Equal(DbTypeCategory.Temporal, ColumnCategory.Of(Dbms.Oracle, dataType));

    [Theory]
    [InlineData("INTEGER", DbTypeCategory.Numeric)]
    [InlineData("BIGINT", DbTypeCategory.Numeric)]
    [InlineData("VARCHAR(50)", DbTypeCategory.Text)]
    [InlineData("CLOB", DbTypeCategory.Text)]
    [InlineData("BLOB", DbTypeCategory.Binary)]
    [InlineData("", DbTypeCategory.Binary)]
    [InlineData("REAL", DbTypeCategory.Numeric)]
    [InlineData("DOUBLE", DbTypeCategory.Numeric)]
    public void Of_Sqlite_DeclaredType_ReturnsAffinityCategory(string dataType, DbTypeCategory expected) =>
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
